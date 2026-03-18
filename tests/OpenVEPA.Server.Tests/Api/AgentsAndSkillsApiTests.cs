using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;
using OpenVEPA.Core.Skills;
using OpenVEPA.Server.Api;
using OpenVEPA.Skills.Runtime;

namespace OpenVEPA.Server.Tests.Api;

/// <summary>Integration tests for the agents and skills REST API endpoints.</summary>
public sealed class AgentsAndSkillsApiTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>Initializes a new instance of the <see cref="AgentsAndSkillsApiTests"/> class.</summary>
    public AgentsAndSkillsApiTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"openvepa-api-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public async Task AgentsAndSkillsApis_RequireAuthorization()
    {
        await using var host = await CreateHostAsync().ConfigureAwait(true);

        using var agentsResponse = await host.Client.GetAsync("/api/agents").ConfigureAwait(true);
        using var skillsResponse = await host.Client.GetAsync("/api/skills").ConfigureAwait(true);

        agentsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        skillsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AgentsApi_ReturnsSummaries_AndDetail()
    {
        await using var host = await CreateHostAsync().ConfigureAwait(true);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestAuthenticationHandler.ValidToken);

        using var listResponse = await host.Client.GetAsync("/api/agents").ConfigureAwait(true);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var listDocument = JsonDocument.Parse(listJson);
        var summaries = listDocument.RootElement.EnumerateArray().ToArray();
        summaries.Should().HaveCount(2);

        // Built-in system agent is always first.
        summaries[0].GetProperty("name").GetString().Should().Be("assistant");
        summaries[0].GetProperty("isSystem").GetBoolean().Should().BeTrue();

        // Filesystem-discovered agent follows.
        summaries[1].GetProperty("name").GetString().Should().Be("planner");
        summaries[1].GetProperty("description").GetString().Should().Be("Plans work in structured milestones.");
        summaries[1].GetProperty("autonomyLevel").GetInt32().Should().Be(2);
        summaries[1].GetProperty("skillCount").GetInt32().Should().Be(2);
        summaries[1].GetProperty("isSystem").GetBoolean().Should().BeFalse();
        summaries[1].TryGetProperty("systemPrompt", out _).Should().BeFalse();

        using var detailResponse = await host.Client.GetAsync("/api/agents/planner").ConfigureAwait(true);

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailJson = await detailResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var detailDocument = JsonDocument.Parse(detailJson);
        detailDocument.RootElement.GetProperty("name").GetString().Should().Be("planner");
        detailDocument.RootElement.TryGetProperty("systemPrompt", out _).Should().BeFalse();
        detailDocument.RootElement.GetProperty("skills").EnumerateArray().Select(static skill => skill.GetString()).Should().Equal("summarize", "classify");

        using var missingResponse = await host.Client.GetAsync("/api/agents/missing").ConfigureAwait(true);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SkillsApi_ReturnsSummaries_AndDetail()
    {
        await using var host = await CreateHostAsync().ConfigureAwait(true);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestAuthenticationHandler.ValidToken);

        using var listResponse = await host.Client.GetAsync("/api/skills").ConfigureAwait(true);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var listDocument = JsonDocument.Parse(listJson);
        var summaries = listDocument.RootElement.EnumerateArray().ToArray();
        summaries.Should().HaveCount(1);
        summaries[0].GetProperty("name").GetString().Should().Be("summarize");
        summaries[0].GetProperty("description").GetString().Should().Be("Summarizes text.");
        summaries[0].GetProperty("version").GetString().Should().Be("1.2.3");
        summaries[0].GetProperty("type").GetInt32().Should().Be((int)SkillType.McpBridge);

        using var detailResponse = await host.Client.GetAsync("/api/skills/summarize").ConfigureAwait(true);

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailJson = await detailResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var detailDocument = JsonDocument.Parse(detailJson);
        detailDocument.RootElement.GetProperty("name").GetString().Should().Be("summarize");
        detailDocument.RootElement.GetProperty("inputs").GetArrayLength().Should().Be(1);
        detailDocument.RootElement.GetProperty("outputs").GetArrayLength().Should().Be(1);
        detailDocument.RootElement.GetProperty("permissions").ValueKind.Should().Be(JsonValueKind.Object);

        using var missingResponse = await host.Client.GetAsync("/api/skills/missing").ConfigureAwait(true);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ApiTestHost> CreateHostAsync()
    {
        var rootPath = Path.Combine(_tempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        var agentsPath = Path.Combine(rootPath, "agents");
        Directory.CreateDirectory(agentsPath);
        CreateAgentDefinition(agentsPath);

        var skillsPath = Path.Combine(rootPath, "skills");
        Directory.CreateDirectory(skillsPath);
        CreateSkillManifest(skillsPath);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = rootPath,
        });

        builder.Services.AddRouting();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddLogging();
        builder.Logging.ClearProviders();

        builder.Services.Configure<AgentOptions>(options =>
        {
            options.AgentsDirectory = agentsPath;
            options.DefaultAgent = "planner";
        });
        builder.Services.AddSingleton<AgentMdParser>();
        builder.Services.AddSingleton<AgentDirectory>();
        builder.Services.AddSingleton<IUserProfileService, StubUserProfileService>();
        builder.Services.AddSingleton<IAgentTokenBudgetTracker, StubAgentTokenBudgetTracker>();

        builder.Services.Configure<SkillsOptions>(options => options.SkillsDirectory = skillsPath);
        builder.Services.AddSingleton<SkillMdParser>();
        builder.Services.AddSingleton<SkillDirectory>();
        builder.Services.AddSingleton<NativeSkillLoader>();
        builder.Services.AddSingleton<ISkillRuntime, SkillRuntime>();

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAgentsApiEndpoints();
        app.MapSkillsApiEndpoints();

        await app.StartAsync().ConfigureAwait(true);

        var server = app.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
        var baseAddress = new Uri(addressFeature.Addresses.First());
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var client = new HttpClient(handler) { BaseAddress = baseAddress };

        return new ApiTestHost(app, client);
    }

    private static void CreateAgentDefinition(string agentsPath)
    {
        var agentDirectory = Path.Combine(agentsPath, "planner");
        Directory.CreateDirectory(agentDirectory);
        File.WriteAllText(Path.Combine(agentDirectory, "planner.agent.md"), """
            ---
            name: planner
            description: Plans work in structured milestones.
            openvepa-skills:
              - summarize
              - classify
            openvepa-autonomy-default: 2
            openvepa-llm-requirements:
              capabilities:
                - reasoning
            ---
            ## System Prompt
            Plan work carefully.

            ## Guidance
            Break work into milestones.
            """);
    }

    private static void CreateSkillManifest(string skillsPath)
    {
        var skillDirectory = Path.Combine(skillsPath, "summarize");
        Directory.CreateDirectory(skillDirectory);
        File.WriteAllText(Path.Combine(skillDirectory, "SKILL.md"), """
            ---
            name: summarize
            description: Summarizes text.
            version: 1.2.3
            openvepa-type: mcp-bridge
            inputs:
              - name: text
                type: string
                description: Text to summarize
                required: true
            outputs:
              - name: summary
                type: string
                description: Summary text
            permissions:
              allowed-tools:
                - read
            ---
            # Summarize
            """);
    }

    private sealed class ApiTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public ApiTestHost(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public HttpClient Client { get; }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync().ConfigureAwait(true);
            await _app.DisposeAsync().ConfigureAwait(true);
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
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

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
                new Claim(ClaimTypes.Name, "Integration Test User"),
            ],
            SchemeName);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubUserProfileService : IUserProfileService
    {
        public Task<UserPreferences> GetRelevantPreferencesAsync(string? taskDomain, CancellationToken ct) =>
            Task.FromResult(new UserPreferences(new Dictionary<string, PreferenceEntry>()));

        public Task SetExplicitPreferenceAsync(string key, string value, string category, CancellationToken ct) =>
            Task.CompletedTask;

        public Task DeletePreferenceAsync(string key, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<PreferenceEntry>> GetAllPreferencesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PreferenceEntry>>([]);
    }

    private sealed class StubAgentTokenBudgetTracker : IAgentTokenBudgetTracker
    {
        public Task<BudgetCheckResult> CheckBudgetAsync(string agentName, AgentTokenBudget? budget, CancellationToken ct = default) =>
            Task.FromResult(new BudgetCheckResult(true, false, null, 0, 0, null));

        public Task RecordUsageAsync(string agentName, int inputTokens, int outputTokens, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<AgentTokenUsageSummary> GetUsageSummaryAsync(string agentName, AgentTokenBudget? budget, CancellationToken ct = default) =>
            Task.FromResult(new AgentTokenUsageSummary(agentName, 0, 0, 0, 0, BudgetPeriod.Monthly, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1)));
    }
}

