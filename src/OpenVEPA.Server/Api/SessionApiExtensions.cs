using System.ClientModel;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;

namespace OpenVEPA.Server.Api;

/// <summary>Maps REST API endpoints for conversation sessions and messages.</summary>
internal static class SessionApiExtensions
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;
    private const string UpdateNotSupportedMessage =
        "ISessionStore does not define a session update operation.";
    private const string DeleteNotSupportedMessage =
        "ISessionStore does not define a session delete or archive operation.";

    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/sessions</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapSessionApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder sessions = app.MapGroup("/api/sessions")
            .RequireAuthorization();

        sessions.MapGet(string.Empty, async (ISessionStore sessionStore, CancellationToken ct) =>
        {
            var items = await sessionStore.ListSessionsAsync(ct).ConfigureAwait(false);
            return Results.Ok(items.OrderByDescending(static session => session.UpdatedAt).ToArray());
        });

        sessions.MapPost(string.Empty, async (
            CreateSessionRequest? request,
            ISessionStore sessionStore,
            CancellationToken ct) =>
        {
            var session = await sessionStore.CreateSessionAsync(
                    NormalizeTitle(request?.Title),
                    ct)
                .ConfigureAwait(false);

            return Results.Created($"/api/sessions/{session.Id}", session);
        });

        sessions.MapGet("/{id}", async (string id, ISessionStore sessionStore, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(new { error = "Session id is required." });
            }

            var session = await sessionStore.GetSessionAsync(id, ct).ConfigureAwait(false);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        sessions.MapPut("/{id}", () => Results.Json(
            new { error = UpdateNotSupportedMessage },
            statusCode: StatusCodes.Status501NotImplemented));

        sessions.MapDelete("/{id}", () => Results.Json(
            new { error = DeleteNotSupportedMessage },
            statusCode: StatusCodes.Status501NotImplemented));

        sessions.MapGet("/{id}/messages", async (
            string id,
            ISessionStore sessionStore,
            CancellationToken ct,
            int page = 1,
            int pageSize = DefaultPageSize) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(new { error = "Session id is required." });
            }

            if (page < 1)
            {
                return Results.BadRequest(new { error = "Page must be greater than or equal to 1." });
            }

            if (pageSize is < 1 or > MaxPageSize)
            {
                return Results.BadRequest(new { error = $"Page size must be between 1 and {MaxPageSize}." });
            }

            var session = await sessionStore.GetSessionAsync(id, ct).ConfigureAwait(false);
            if (session is null)
            {
                return Results.NotFound();
            }

            var skipLong = ((long)page - 1) * pageSize;
            if (skipLong > int.MaxValue)
            {
                return Results.BadRequest(new { error = "Requested page is too large." });
            }

            var pageOfMessages = await sessionStore.GetMessagesAsync(
                    id,
                    (int)skipLong,
                    pageSize + 1,
                    ct)
                .ConfigureAwait(false);

            var hasMore = pageOfMessages.Count > pageSize;
            var messages = hasMore
                ? pageOfMessages.Take(pageSize).ToArray()
                : pageOfMessages;

            return Results.Ok(new SessionMessagesPageResponse(messages, page, pageSize, hasMore));
        });

        sessions.MapPost("/{id}/messages", async (
            string id,
            SendSessionMessageRequest? request,
            ISessionStore sessionStore,
            IAgentRuntime agentRuntime,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(SessionApiExtensions));

            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(new { error = "Session id is required." });
            }

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var session = await sessionStore.GetSessionAsync(id, ct).ConfigureAwait(false);
                if (session is null)
                {
                    return Results.NotFound();
                }

                logger.LogDebug("Processing message for session {SessionId}", id);

                var userMessage = CreateMessage(id, MessageRole.User, request.Message, usage: null);
                await sessionStore.AddMessageAsync(userMessage, ct).ConfigureAwait(false);

                AgentResponse response;
                try
                {
                    response = await agentRuntime.ProcessMessageAsync(id, request.Message, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process message in session {SessionId}. ExceptionType={ExType}, Message={ExMsg}",
                        id, ex.GetType().Name, ex.Message);

                    if (ex is ClientResultException cre)
                    {
                        logger.LogError(
                            "LLM provider returned HTTP {Status}. Full response: {FullMessage}",
                            cre.Status, cre.Message);
                    }

                    var errorContent = FormatChatError(ex);
                    var errorMessage = CreateMessage(id, MessageRole.Assistant, errorContent, usage: null);

                    try
                    {
                        await sessionStore.AddMessageAsync(errorMessage, ct).ConfigureAwait(false);
                    }
                    catch (Exception storeEx)
                    {
                        logger.LogError(storeEx,
                            "Failed to persist error message for session {SessionId}", id);
                    }

                    return Results.Ok(errorMessage);
                }

                var assistantMessage = CreateMessage(
                    id,
                    MessageRole.Assistant,
                    response.Content,
                    response.TotalTokenUsage);

                await sessionStore.AddMessageAsync(assistantMessage, ct).ConfigureAwait(false);

                logger.LogDebug("Message processed successfully for session {SessionId}", id);

                return Results.Ok(assistantMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unhandled error in chat endpoint for session {SessionId}", id);

                return Results.Json(
                    new { error = $"An internal error occurred: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        return app;
    }

    private static Message CreateMessage(
        string sessionId,
        MessageRole role,
        string content,
        TokenUsage? usage)
    {
        return new Message(
            Id: Guid.NewGuid().ToString("N"),
            SessionId: sessionId,
            Role: role,
            Content: content,
            Timestamp: DateTime.UtcNow,
            Usage: usage);
    }

    private static string? NormalizeTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    }

    private static string FormatChatError(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚠️ **Unable to process your message**");
        sb.AppendLine();

        if (ex is InvalidOperationException ioe)
        {
            if (ioe.Message.Contains("IChatClient", StringComparison.OrdinalIgnoreCase)
                || ioe.Message.Contains("provider", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("**Cause:** No LLM provider is configured or the configured provider could not be found.");
                sb.AppendLine();
                sb.AppendLine("**How to fix:**");
                sb.AppendLine("1. Go to **LLM Settings** in the sidebar and add a provider with a valid API key");
                sb.AppendLine("2. Then go to **Agents** → select this agent → configure the Provider and Model");
            }
            else if (ioe.Message.Contains("API key", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("**Cause:** The LLM provider's API key is missing or invalid.");
                sb.AppendLine();
                sb.AppendLine("**How to fix:** Go to **LLM Settings** and update the API key for your provider.");
            }
            else
            {
                sb.AppendLine($"**Cause:** {ex.Message}");
            }
        }
        else if (ex is ClientResultException cre)
        {
            switch (cre.Status)
            {
                case 401 or 403:
                    sb.AppendLine("**Cause:** The LLM provider rejected the API key. Check that the API key is valid and has the correct permissions.");
                    sb.AppendLine();
                    sb.AppendLine("**How to fix:** Go to **LLM Settings** and verify the API key for your provider.");
                    break;
                case 404:
                    sb.AppendLine("**Cause:** The LLM provider endpoint or model was not found. This usually means the API endpoint URL is incorrect or the model name is not supported by this provider.");
                    sb.AppendLine();
                    sb.AppendLine("**How to fix:**");
                    sb.AppendLine("1. Go to **LLM Settings** and verify the provider configuration");
                    sb.AppendLine("2. Run the test-chat diagnostic: `POST /api/system/test-chat`");
                    sb.AppendLine("3. Check `/api/system/diagnostics` for the resolved endpoint and model");
                    break;
                case 429:
                    sb.AppendLine("**Cause:** Rate limit exceeded. The LLM provider is throttling requests. Wait a moment and try again.");
                    break;
                case >= 500:
                    sb.AppendLine("**Cause:** The LLM provider returned a server error. This is usually temporary — try again in a few moments.");
                    break;
                default:
                    sb.AppendLine($"**Cause:** The LLM provider returned HTTP {cre.Status}.");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine($"**Details:** {cre.Message}");
        }
        else if (ex is HttpRequestException)
        {
            sb.AppendLine("**Cause:** Could not connect to the LLM provider.");
            sb.AppendLine();
            sb.AppendLine("**How to fix:**");
            sb.AppendLine("- Check that the provider endpoint is reachable");
            sb.AppendLine("- For Ollama: ensure the Ollama service is running");
            sb.AppendLine($"- Details: {ex.Message}");
        }
        else
        {
            sb.AppendLine($"**Error:** {ex.Message}");
        }

        return sb.ToString();
    }
}

/// <summary>Represents a request to create a conversation session.</summary>
internal sealed record CreateSessionRequest
{
    /// <summary>Gets the optional session title.</summary>
    public string? Title { get; init; }
}

/// <summary>Represents a request to send a message to a conversation session.</summary>
internal sealed record SendSessionMessageRequest
{
    /// <summary>Gets the user message text.</summary>
    public string? Message { get; init; }
}

/// <summary>Represents a paged response of session messages.</summary>
internal sealed record SessionMessagesPageResponse(
    IReadOnlyList<Message> Messages,
    int Page,
    int PageSize,
    bool HasMore);
