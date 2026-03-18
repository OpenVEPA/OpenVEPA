using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenVEPA.Server;
using OpenVEPA.Server.Middleware;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Tests;

/// <summary>
/// Unit and integration tests for the unified WebUI — setup wizard redirect, dashboard,
/// and setup API endpoints served from a single server.
/// </summary>
public sealed class UnifiedWebUiTests : IDisposable
{
    private readonly string _tempDir;

    public UnifiedWebUiTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"openvepa-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // ─── SetupCompletionService ───────────────────────────────────────────

    [Fact]
    public void IsSetupComplete_ReturnsFalse_WhenConfigFileMissing()
    {
        var svc = new SetupCompletionService(_tempDir);

        svc.IsSetupComplete.Should().BeFalse();
    }

    [Fact]
    public void IsSetupComplete_ReturnsTrue_WhenConfigFileExists()
    {
        File.WriteAllText(Path.Combine(_tempDir, "appsettings.json"), "{}");

        var svc = new SetupCompletionService(_tempDir);

        svc.IsSetupComplete.Should().BeTrue();
    }

    [Fact]
    public void MarkComplete_SetsIsSetupCompleteToTrue()
    {
        var svc = new SetupCompletionService(_tempDir);
        svc.IsSetupComplete.Should().BeFalse();

        svc.MarkComplete();

        svc.IsSetupComplete.Should().BeTrue();
    }

    [Fact]
    public void Refresh_RechecksFileSystem()
    {
        var svc = new SetupCompletionService(_tempDir);
        svc.IsSetupComplete.Should().BeFalse();

        File.WriteAllText(Path.Combine(_tempDir, "appsettings.json"), "{}");
        svc.Refresh();

        svc.IsSetupComplete.Should().BeTrue();
    }

    // ─── SetupRedirectMiddleware ──────────────────────────────────────────

    [Fact]
    public async Task Redirects_Root_WhenSetupIncomplete()
    {
        var middleware = CreateMiddleware(setupComplete: false);
        var context = CreateHttpContext("/");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(302);
        context.Response.Headers.Location.ToString().Should().Be("/init/setupwizard");
    }

    [Fact]
    public async Task Redirects_ArbitraryPath_WhenSetupIncomplete()
    {
        var middleware = CreateMiddleware(setupComplete: false);
        var context = CreateHttpContext("/some/path");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(302);
        context.Response.Headers.Location.ToString().Should().Be("/init/setupwizard");
    }

    [Fact]
    public async Task AllowsThrough_InitPaths_WhenSetupIncomplete()
    {
        var middleware = CreateMiddleware(setupComplete: false);
        var context = CreateHttpContext("/init/setupwizard");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AllowsThrough_HealthEndpoint_WhenSetupIncomplete()
    {
        var middleware = CreateMiddleware(setupComplete: false);
        var context = CreateHttpContext("/health");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task PassesThrough_AllRequests_WhenSetupComplete()
    {
        var middleware = CreateMiddleware(setupComplete: true);
        var context = CreateHttpContext("/");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    // ─── Integration Tests ────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_Returns200_WithHtmlContent_WhenSetupComplete()
    {
        await RunIntegrationTestAsync(setupComplete: true, async client =>
        {
            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("OpenVEPA WebUI");
        });
    }

    [Fact]
    public async Task SetupWizard_Returns200_WithHtmlContent_WhenSetupIncomplete()
    {
        await RunIntegrationTestAsync(setupComplete: false, async client =>
        {
            // /init/setupwizard is allowed through the redirect middleware regardless of setup state.
            var response = await client.GetAsync("/init/setupwizard");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("<html");
        });
    }

    [Fact]
    public async Task StatusEndpoint_ReturnsJson()
    {
        await RunIntegrationTestAsync(setupComplete: true, async client =>
        {
            var response = await client.GetAsync("/api/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            root.TryGetProperty("status", out _).Should().BeTrue();
            root.TryGetProperty("version", out _).Should().BeTrue();
            root.TryGetProperty("provider", out _).Should().BeTrue();
            root.TryGetProperty("model", out _).Should().BeTrue();
        });
    }

    [Fact]
    public async Task ModelProxy_ReturnsBadRequest_WithoutProvider()
    {
        await RunIntegrationTestAsync(setupComplete: true, async client =>
        {
            var response = await client.GetAsync("/init/api/models");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        });
    }

    [Fact]
    public async Task SetupApi_CreatesBootstrapToken_AndReturnsIt()
    {
        await RunIntegrationTestAsync(setupComplete: false, async (client, tokenStore, homeDir) =>
        {
            using var response = await client.PostAsJsonAsync("/init/api/setup", new
            {
                provider = "ollama",
                modelId = "llama3.2",
                endpoint = "http://localhost:11434",
                homePath = homeDir,
                port = 8371,
                schedulerEnabled = true,
                telegramBotToken = string.Empty,
                whatsAppEnabled = false,
            }).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            var root = document.RootElement;
            root.GetProperty("redirectUrl").GetString().Should().Be("/");
            root.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
            root.GetProperty("tokenId").GetString().Should().NotBeNullOrWhiteSpace();

            var tokens = await tokenStore.ListTokensAsync().ConfigureAwait(false);
            tokens.Should().ContainSingle();
            tokens[0].Name.Should().Be("web-default");
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private SetupRedirectMiddleware CreateMiddleware(bool setupComplete)
    {
        if (setupComplete)
        {
            File.WriteAllText(Path.Combine(_tempDir, "appsettings.json"), "{}");
        }

        var service = new SetupCompletionService(_tempDir);
        return new SetupRedirectMiddleware(
            ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            service);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }

    /// <summary>
    /// Creates a minimal Kestrel test server with setup endpoints, starts it on a random port,
    /// runs the test delegate, then tears down the server.
    /// </summary>
    private Task RunIntegrationTestAsync(bool setupComplete, Func<HttpClient, Task> test)
    {
        return RunIntegrationTestAsync(
            setupComplete,
            async (client, _, _) => await test(client).ConfigureAwait(false));
    }

    /// <summary>
    /// Creates a minimal Kestrel test server with setup endpoints, starts it on a random port,
    /// exposes the token store for assertions, then tears down the server.
    /// </summary>
    private async Task RunIntegrationTestAsync(
        bool setupComplete,
        Func<HttpClient, SqliteTokenStore, string, Task> test)
    {
        var homeDir = Path.Combine(_tempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(homeDir);

        if (setupComplete)
        {
            File.WriteAllText(Path.Combine(homeDir, "appsettings.json"), "{}");
        }

        var setupService = new SetupCompletionService(homeDir);
        var dataDir = Path.Combine(homeDir, "data");
        Directory.CreateDirectory(dataDir);
        var documentsPath = Path.Combine(dataDir, "documents");
        Directory.CreateDirectory(documentsPath);
        var connectionString = $"Data Source={Path.Combine(dataDir, "openvepa.db")}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = homeDir,
        });

        builder.Services.AddSingleton(setupService);
        builder.Services.AddOpenVepaStorage(connectionString, documentsPath);
        builder.Services.AddHealthChecks();
        builder.Services.AddRouting();
        builder.Logging.ClearProviders();

        await using var app = builder.Build();
        await app.Services.InitializeStorageAsync().ConfigureAwait(false);

        app.Urls.Add("http://127.0.0.1:0");

        app.UseMiddleware<SetupRedirectMiddleware>();

        app.MapSetupApiEndpoints();
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Content(DashboardPageHtml.Content, "text/html"));
        app.MapGet("/init/setupwizard", () =>
            Results.Content("<html><body>Setup Wizard</body></html>", "text/html"));
        app.MapGet("/api/status", (SetupCompletionService svc,
            IConfiguration config) =>
        {
            var provider = config["Providers:DefaultProvider"] ?? "unknown";
            var model = config["Providers:Ollama:ModelId"]
                        ?? config["Providers:OpenAi:ModelId"]
                        ?? "unknown";
            return Results.Ok(new
            {
                status = "running",
                version = "1.0.0",
                provider,
                model,
                setupComplete = svc.IsSetupComplete,
            });
        });

        await app.StartAsync().ConfigureAwait(false);
        try
        {
            var server = app.Services.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
            var baseAddress = new Uri(addressFeature.Addresses.First());
            var tokenStore = app.Services.GetRequiredService<SqliteTokenStore>();

            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { BaseAddress = baseAddress };

            await test(client, tokenStore, homeDir).ConfigureAwait(false);
        }
        finally
        {
            await app.StopAsync().ConfigureAwait(false);
        }
    }
}
