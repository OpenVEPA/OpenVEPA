using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(new { error = "Session id is required." });
            }

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            var session = await sessionStore.GetSessionAsync(id, ct).ConfigureAwait(false);
            if (session is null)
            {
                return Results.NotFound();
            }

            var userMessage = CreateMessage(id, MessageRole.User, request.Message, usage: null);
            await sessionStore.AddMessageAsync(userMessage, ct).ConfigureAwait(false);

            var response = await agentRuntime.ProcessMessageAsync(id, request.Message, ct)
                .ConfigureAwait(false);

            var assistantMessage = CreateMessage(
                id,
                MessageRole.Assistant,
                response.Content,
                response.TotalTokenUsage);

            await sessionStore.AddMessageAsync(assistantMessage, ct).ConfigureAwait(false);

            return Results.Ok(assistantMessage);
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
