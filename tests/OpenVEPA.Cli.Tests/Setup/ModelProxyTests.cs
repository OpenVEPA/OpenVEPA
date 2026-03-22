using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using OpenVEPA.Cli.Setup;

namespace OpenVEPA.Cli.Tests.Setup;

/// <summary>
/// Integration tests for the <c>/api/models</c> proxy endpoint in the setup wizard.
/// A fake upstream provider server supplies canned responses so no real external
/// API calls are made.
/// </summary>
public sealed class ModelProxyTests : IAsyncLifetime
{
    private WebApplication? _wizardApp;
    private WebApplication? _fakeUpstreamApp;
    private HttpClient? _client;
    private int _fakeUpstreamPort;

    public async Task InitializeAsync()
    {
        // --- Fake upstream provider server ---
        _fakeUpstreamPort = FindFreePort();
        var upstreamBuilder = WebApplication.CreateSlimBuilder();
        upstreamBuilder.WebHost.UseUrls($"http://localhost:{_fakeUpstreamPort}");
        upstreamBuilder.Logging.ClearProviders();
        _fakeUpstreamApp = upstreamBuilder.Build();

        // Ollama-style response
        _fakeUpstreamApp.MapGet("/api/tags", () => Results.Json(new
        {
            models = new[]
            {
                new { name = "llama3:latest" },
                new { name = "codellama:7b" },
            },
        }));

        // Combined OpenAI-compatible + Google-style response.
        // OpenAI-compatible providers read "data"; Google reads "models".
        _fakeUpstreamApp.MapGet("/models", () => Results.Json(new
        {
            data = new[]
            {
                new { id = "gpt-4o" },
                new { id = "gpt-3.5-turbo" },
                new { id = "dall-e-3" },
            },
            models = new[]
            {
                new { name = "models/gemini-pro" },
                new { name = "models/gemini-1.5-flash" },
                new { name = "models/text-bison-001" },
            },
        }));

        await _fakeUpstreamApp.StartAsync();

        // --- Wizard web app with production endpoints ---
        var wizardPort = FindFreePort();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://localhost:{wizardPort}");
        builder.Logging.ClearProviders();
        _wizardApp = builder.Build();

        var tcs = new TaskCompletionSource<SetupConfiguration?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        typeof(SetupWizardWebUi)
            .GetMethod("MapEndpoints", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [_wizardApp, tcs]);

        await _wizardApp.StartAsync();

        _client = new HttpClient { BaseAddress = new Uri($"http://localhost:{wizardPort}") };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        try
        {
            if (_wizardApp is not null)
            {
                await _wizardApp.StopAsync();
                await _wizardApp.DisposeAsync();
            }
        }
        finally
        {
            if (_fakeUpstreamApp is not null)
            {
                await _fakeUpstreamApp.StopAsync();
                await _fakeUpstreamApp.DisposeAsync();
            }
        }
    }

    #region Parameter validation

    [Fact]
    public async Task Missing_Provider_Returns_BadRequest()
    {
        var response = await _client!.GetAsync("/api/models");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await ReadJsonBodyAsync(response);
        body.GetProperty("error").GetString()
            .Should().Be("Missing required parameter: provider");
    }

    [Fact]
    public async Task Empty_Provider_Returns_BadRequest()
    {
        var response = await _client!.GetAsync("/api/models?provider=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("azure")]
    public async Task Unsupported_Provider_Returns_Ok_With_Error(string provider)
    {
        var response = await _client!.GetAsync($"/api/models?provider={provider}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetString()
            .Should().Contain("does not support");
    }

    [Fact]
    public async Task Unknown_Provider_Returns_Ok_With_Error()
    {
        var response = await _client!.GetAsync("/api/models?provider=nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonBodyAsync(response);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region Happy-path via fake upstream

    [Fact]
    public async Task Ollama_Returns_Sorted_Models()
    {
        var response = await _client!.GetAsync(
            $"/api/models?provider=ollama&endpoint=http://localhost:{_fakeUpstreamPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        var models = GetStringArray(body, "models");

        models.Should().Equal("codellama:7b", "llama3:latest");
    }

    [Fact]
    public async Task OpenAi_Filters_Non_Gpt_Models()
    {
        var response = await _client!.GetAsync(
            $"/api/models?provider=openai&apiKey=test&endpoint=http://localhost:{_fakeUpstreamPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        var models = GetStringArray(body, "models");

        models.Should().NotContain("dall-e-3");
        models.Should().Equal("gpt-3.5-turbo", "gpt-4o");
    }

    [Fact]
    public async Task Google_Strips_Prefix_And_Filters_Non_Gemini()
    {
        var response = await _client!.GetAsync(
            $"/api/models?provider=google&apiKey=test&endpoint=http://localhost:{_fakeUpstreamPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        var models = GetStringArray(body, "models");

        models.Should().NotContain("text-bison-001");
        models.Should().Equal("gemini-1.5-flash", "gemini-pro");
    }

    [Fact]
    public async Task Non_OpenAi_Provider_Returns_All_Models_Unfiltered()
    {
        var response = await _client!.GetAsync(
            $"/api/models?provider=mistral&apiKey=test&endpoint=http://localhost:{_fakeUpstreamPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        var models = GetStringArray(body, "models");

        models.Should().HaveCount(3).And.Contain("dall-e-3");
    }

    [Theory]
    [InlineData("groq")]
    [InlineData("together")]
    [InlineData("perplexity")]
    public async Task Other_Supported_Providers_Return_OK(string provider)
    {
        var response = await _client!.GetAsync(
            $"/api/models?provider={provider}&apiKey=test&endpoint=http://localhost:{_fakeUpstreamPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error handling

    [Fact]
    public async Task Unreachable_Upstream_Returns_Ok_With_Error()
    {
        var deadPort = FindFreePort();

        var response = await _client!.GetAsync(
            $"/api/models?provider=ollama&endpoint=http://localhost:{deadPort}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadJsonBodyAsync(response);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Unreachable_Upstream_Error_Contains_Diagnostics()
    {
        var deadPort = FindFreePort();

        var response = await _client!.GetAsync(
            $"/api/models?provider=ollama&endpoint=http://localhost:{deadPort}");

        var body = await ReadJsonBodyAsync(response);
        body.GetProperty("diagnostics").GetString()
            .Should().Contain("Could not connect");
    }

    #endregion

    #region Helpers

    private static async Task<JsonElement> ReadJsonBodyAsync(HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }

    private static List<string> GetStringArray(JsonElement root, string propertyName)
    {
        return root.GetProperty(propertyName)
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToList();
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    #endregion
}
