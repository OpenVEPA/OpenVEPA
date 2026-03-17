using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;
using OpenVEPA.Server;
using OpenVEPA.Server.Api;
using OpenVEPA.Storage;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Server.Tests.Api;

/// <summary>Integration tests for the session REST API endpoints.</summary>
public sealed class SessionApiIntegrationTests
{
    [Fact]
    public async Task GetSessions_ReturnsUnauthorized_WhenAuthenticationIsMissing()
    {
        await using var host = await SessionApiTestHost.StartAsync();

        using var response = await host.Client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessions_ReturnsEmptyArray_WhenNoSessionsExist()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.Content.ReadFromJsonAsync<IReadOnlyList<Session>>();

        sessions.Should().NotBeNull();
        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task PostSessions_CreatesSession_AndReturnsCreatedResource()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.PostAsJsonAsync("/api/sessions", new { title = "Daily plan" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await response.Content.ReadFromJsonAsync<Session>();

        session.Should().NotBeNull();
        session!.Id.Should().NotBeNullOrWhiteSpace();
        session.Title.Should().Be("Daily plan");
        session.Status.Should().Be(SessionStatus.Active);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/sessions/{session.Id}");
    }

    [Fact]
    public async Task GetSessionById_ReturnsSpecificSession()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();
        var created = await host.SessionStore.CreateSessionAsync("Investigation");

        using var response = await host.Client.GetAsync($"/api/sessions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<Session>();

        session.Should().NotBeNull();
        session.Should().Be(created);
    }

    [Fact]
    public async Task GetSessionById_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.GetAsync("/api/sessions/missing-session");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSessionMessages_ReturnsPaginatedMessages()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();
        var session = await host.SessionStore.CreateSessionAsync("Conversation");
        await host.SessionStore.AddMessageAsync(new Message(
            Id: "message-1",
            SessionId: session.Id,
            Role: MessageRole.User,
            Content: "First",
            Timestamp: new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Usage: null));
        await host.SessionStore.AddMessageAsync(new Message(
            Id: "message-2",
            SessionId: session.Id,
            Role: MessageRole.Assistant,
            Content: "Second",
            Timestamp: new DateTime(2025, 01, 01, 0, 1, 0, DateTimeKind.Utc),
            Usage: new TokenUsage(4, 6, 0.01m)));
        await host.SessionStore.AddMessageAsync(new Message(
            Id: "message-3",
            SessionId: session.Id,
            Role: MessageRole.User,
            Content: "Third",
            Timestamp: new DateTime(2025, 01, 01, 0, 2, 0, DateTimeKind.Utc),
            Usage: null));

        using var response = await host.Client.GetAsync($"/api/sessions/{session.Id}/messages?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<SessionMessagesPageResponse>();

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.HasMore.Should().BeTrue();
        page.Messages.Select(static message => message.Content).Should().Equal("First", "Second");
        var secondMessageUsage = page.Messages[1].Usage;
        secondMessageUsage.Should().NotBeNull();
        secondMessageUsage!.InputTokens.Should().Be(4);
        secondMessageUsage.OutputTokens.Should().Be(6);
    }

    [Fact]
    public async Task PostSessionMessage_PersistsUserAndAssistantMessages()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();
        var session = await host.SessionStore.CreateSessionAsync("Chat");
        host.AgentRuntime.Response = new AgentResponse(
            Content: "Hello from agent",
            SkillResults: null,
            TotalTokenUsage: new TokenUsage(11, 17, 0.25m));

        using var response = await host.Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/messages",
            new { message = "Hello server" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assistantMessage = await response.Content.ReadFromJsonAsync<Message>();

        assistantMessage.Should().NotBeNull();
        assistantMessage!.Role.Should().Be(MessageRole.Assistant);
        assistantMessage.Content.Should().Be("Hello from agent");
        assistantMessage.Usage.Should().NotBeNull();
        assistantMessage.Usage!.InputTokens.Should().Be(11);
        assistantMessage.Usage.OutputTokens.Should().Be(17);

        host.AgentRuntime.Requests.Should().ContainSingle();
        host.AgentRuntime.Requests[0].SessionId.Should().Be(session.Id);
        host.AgentRuntime.Requests[0].Message.Should().Be("Hello server");

        var persistedMessages = await host.SessionStore.GetMessagesAsync(session.Id, 0, 10);
        persistedMessages.Should().HaveCount(2);
        persistedMessages[0].Role.Should().Be(MessageRole.User);
        persistedMessages[0].Content.Should().Be("Hello server");
        persistedMessages[1].Role.Should().Be(MessageRole.Assistant);
        persistedMessages[1].Content.Should().Be("Hello from agent");
    }

    [Fact]
    public async Task UpdateAndDeleteSession_ReturnNotImplemented()
    {
        await using var host = await SessionApiTestHost.StartAsync();
        host.Authorize();
        var session = await host.SessionStore.CreateSessionAsync("Readonly");

        using var updateResponse = await host.Client.PutAsJsonAsync($"/api/sessions/{session.Id}", new { title = "Updated" });
        using var deleteResponse = await host.Client.DeleteAsync($"/api/sessions/{session.Id}");

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}

/// <summary>Integration tests for the system REST API endpoints.</summary>
public sealed class SystemApiIntegrationTests
{
    [Fact]
    public async Task GetSystemStatus_ReturnsExpectedFields()
    {
        await using var host = await SystemApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.GetAsync("/api/system/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();

        status.Should().NotBeNull();
        status!.Version.Should().NotBeNullOrWhiteSpace();
        status.Uptime.Should().NotBeNullOrWhiteSpace();
        status.Provider.Should().Be("ollama");
        status.Model.Should().Be("llama3.2");
        status.SetupComplete.Should().BeTrue();
        status.DatabasePath.Should().Be(Path.Combine(host.RootPath, "data", "openvepa.db"));
        status.ActiveSessionsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLlmConfig_ReturnsProviderInformation()
    {
        await using var host = await SystemApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.GetAsync("/api/system/llm-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configuration = await response.Content.ReadFromJsonAsync<LlmMultiProviderResponse>();

        configuration.Should().NotBeNull();
        configuration!.DefaultProvider.Should().Be("ollama");
        configuration.Providers.Should().ContainSingle();
        configuration.Providers[0].Name.Should().Be("ollama");
        configuration.Providers[0].ModelId.Should().Be("llama3.2");
        configuration.Providers[0].Endpoint.Should().Be("http://localhost:11434");
    }

    [Fact]
    public async Task PutLlmConfig_UpdatesConfiguration_AndSubsequentReads()
    {
        await using var host = await SystemApiTestHost.StartAsync();
        host.Authorize();

        using var updateResponse = await host.Client.PutAsJsonAsync(
            "/api/system/llm-config",
            new
            {
                defaultProvider = "openai",
                providers = new[]
                {
                    new
                    {
                        name = "openai",
                        modelId = "gpt-5-mini",
                        endpoint = "https://api.example.test/v1",
                    },
                },
            });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var update = await updateResponse.Content.ReadFromJsonAsync<UpdateLlmConfigResponse>();

        update.Should().NotBeNull();
        update!.Updated.Should().BeTrue();
        update.RequiresRestart.Should().BeTrue();
        update.Configuration.DefaultProvider.Should().Be("openai");
        update.Configuration.Providers.Should().Contain(p => p.Name == "openai");

        var openaiEntry = update.Configuration.Providers.First(p => p.Name == "openai");
        openaiEntry.ModelId.Should().Be("gpt-5-mini");
        openaiEntry.Endpoint.Should().Be("https://api.example.test/v1");

        using var readResponse = await host.Client.GetAsync("/api/system/llm-config");
        var configuration = await readResponse.Content.ReadFromJsonAsync<LlmMultiProviderResponse>();

        configuration.Should().NotBeNull();
        configuration!.DefaultProvider.Should().Be("openai");
        configuration.Providers.Should().Contain(p => p.Name == "openai");
        configuration.Providers.First(p => p.Name == "openai").ModelId.Should().Be("gpt-5-mini");
        configuration.Providers.First(p => p.Name == "openai").Endpoint.Should().Be("https://api.example.test/v1");
    }

    [Fact]
    public async Task GetLlmUsage_ReturnsAggregatedUsageSummary()
    {
        await using var host = await SystemApiTestHost.StartAsync();
        host.Authorize();

        using var response = await host.Client.GetAsync("/api/system/llm-usage?provider=ollama");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var usage = await response.Content.ReadFromJsonAsync<LlmUsageSummaryResponse>();

        usage.Should().NotBeNull();
        usage!.Provider.Should().Be("ollama");
        usage.CallCount.Should().Be(2);
        usage.InputTokens.Should().Be(17);
        usage.OutputTokens.Should().Be(29);
        usage.TotalTokens.Should().Be(46);
        usage.TotalCostUsd.Should().Be(0.37m);
    }

    [Fact]
    public async Task SystemEndpoints_ReturnUnauthorized_WhenAuthenticationIsMissing()
    {
        await using var host = await SystemApiTestHost.StartAsync();

        using var response = await host.Client.GetAsync("/api/system/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

internal sealed class SessionApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private SessionApiTestHost(WebApplication app, HttpClient client, InMemorySessionStore sessionStore, RecordingAgentRuntime agentRuntime)
    {
        _app = app;
        Client = client;
        SessionStore = sessionStore;
        AgentRuntime = agentRuntime;
    }

    public HttpClient Client { get; }

    public InMemorySessionStore SessionStore { get; }

    public RecordingAgentRuntime AgentRuntime { get; }

    public static async Task<SessionApiTestHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddRouting();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                static _ => { });
        builder.Services.AddAuthorization();

        var sessionStore = new InMemorySessionStore();
        var agentRuntime = new RecordingAgentRuntime();
        builder.Services.AddSingleton<ISessionStore>(sessionStore);
        builder.Services.AddSingleton<IAgentRuntime>(agentRuntime);

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSessionApiEndpoints();

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var baseAddress = new Uri(addresses!.Addresses.First());
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseAddress,
        };

        return new SessionApiTestHost(app, client, sessionStore, agentRuntime);
    }

    public void Authorize()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestAuthenticationHandler.ValidToken);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

internal sealed class SystemApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly SqliteConnection _connection;

    private SystemApiTestHost(
        WebApplication app,
        HttpClient client,
        string rootPath,
        SqliteConnection connection,
        InMemorySessionStore sessionStore)
    {
        _app = app;
        Client = client;
        RootPath = rootPath;
        _connection = connection;
        SessionStore = sessionStore;
    }

    public HttpClient Client { get; }

    public string RootPath { get; }

    public InMemorySessionStore SessionStore { get; }

    public static async Task<SystemApiTestHost> StartAsync()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"openvepa-system-api-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        await File.WriteAllTextAsync(
            Path.Combine(rootPath, "appsettings.json"),
            """
            {
              "Providers": {
                "DefaultProvider": "ollama",
                "Ollama": {
                  "ModelId": "llama3.2",
                  "Endpoint": "http://localhost:11434"
                }
              }
            }
            """);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = rootPath,
        });

        builder.Logging.ClearProviders();
        builder.Services.AddRouting();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                static _ => { });
        builder.Services.AddAuthorization();

        var setupService = new SetupCompletionService(rootPath);
        var sessionStore = new InMemorySessionStore();
        sessionStore.SeedSession("active-session", SessionStatus.Active);
        sessionStore.SeedSession("archived-session", SessionStatus.Archived);

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        builder.Services.AddSingleton(setupService);
        builder.Services.AddSingleton<ISessionStore>(sessionStore);
        builder.Services.AddDbContextFactory<OpenVepaDbContext>(options => options.UseSqlite(connection));

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSystemApiEndpoints();

        await SeedUsageAsync(app.Services);
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var baseAddress = new Uri(addresses!.Addresses.First());
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseAddress,
        };

        return new SystemApiTestHost(app, client, rootPath, connection, sessionStore);
    }

    public void Authorize()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestAuthenticationHandler.ValidToken);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();

        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static async Task SeedUsageAsync(IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<OpenVepaDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        db.LlmAuditLogEntries.AddRange(
            new LlmAuditLogEntity
            {
                Id = "usage-1",
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                Provider = "ollama",
                Model = "llama3.2",
                InputTokens = 10,
                OutputTokens = 20,
                EstimatedCostUsd = 0.20m,
                LatencyMs = 120,
                Success = true,
            },
            new LlmAuditLogEntity
            {
                Id = "usage-2",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Provider = "ollama",
                Model = "llama3.2",
                InputTokens = 7,
                OutputTokens = 9,
                EstimatedCostUsd = 0.17m,
                LatencyMs = 95,
                Success = true,
            },
            new LlmAuditLogEntity
            {
                Id = "usage-3",
                Timestamp = DateTime.UtcNow.AddMinutes(-2),
                Provider = "openai",
                Model = "gpt-5-mini",
                InputTokens = 3,
                OutputTokens = 4,
                EstimatedCostUsd = 0.05m,
                LatencyMs = 80,
                Success = true,
            });
        await db.SaveChangesAsync();
    }
}

internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Message>> _messages = new(StringComparer.Ordinal);

    public Task<Session> CreateSessionAsync(string? title, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var session = new Session(Guid.NewGuid().ToString("N"), title, now, now, SessionStatus.Active);
            _sessions[session.Id] = session;
            _messages[session.Id] = [];
            return Task.FromResult(session);
        }
    }

    public Task<Session?> GetSessionAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _sessions.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            IReadOnlyList<Session> sessions = _sessions.Values
                .OrderByDescending(static session => session.UpdatedAt)
                .ToArray();
            return Task.FromResult(sessions);
        }
    }

    public Task AddMessageAsync(Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_messages.TryGetValue(message.SessionId, out var messages))
            {
                messages = [];
                _messages[message.SessionId] = messages;
            }

            messages.Add(message);

            if (_sessions.TryGetValue(message.SessionId, out var existingSession))
            {
                _sessions[message.SessionId] = existingSession with { UpdatedAt = message.Timestamp };
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Message>> GetMessagesAsync(string sessionId, int skip, int take, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_messages.TryGetValue(sessionId, out var messages))
            {
                return Task.FromResult<IReadOnlyList<Message>>([]);
            }

            IReadOnlyList<Message> page = messages
                .OrderBy(static message => message.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToArray();
            return Task.FromResult(page);
        }
    }

    public Session SeedSession(string? title, SessionStatus status)
    {
        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var session = new Session(Guid.NewGuid().ToString("N"), title, now, now, status);
            _sessions[session.Id] = session;
            _messages[session.Id] = [];
            return session;
        }
    }
}

internal sealed class RecordingAgentRuntime : IAgentRuntime
{
    public AgentResponse Response { get; set; } = new("Default response", null, null);

    public List<AgentRuntimeRequest> Requests { get; } = [];

    public Task<AgentResponse> ProcessMessageAsync(string sessionId, string message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Requests.Add(new AgentRuntimeRequest(sessionId, message));
        return Task.FromResult(Response);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string sessionId,
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _ = sessionId;
        _ = message;
        ct.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield break;
    }
}

internal sealed record AgentRuntimeRequest(string SessionId, string Message);

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string ValidToken = "test-token";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization) ||
            authorization.Count != 1 ||
            !string.Equals(authorization[0], $"Bearer {ValidToken}", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid bearer token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim(ClaimTypes.Name, "Integration Test User"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
