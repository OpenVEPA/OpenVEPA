using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Agents;
using OpenVEPA.Server;
using OpenVEPA.Server.Api;
using OpenVEPA.Server.Auth;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Tests.Api;

/// <summary>Integration tests for token management and setup token flows.</summary>
public sealed class TokenApiIntegrationTests
{
    [Fact]
    public async Task Bootstrap_ReturnsToken_WhenNoTokensExist()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.PostAsJsonAsync("/api/tokens/bootstrap", new { name = "first" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TokenCreateResponse>();

        body.Should().NotBeNull();
        body!.TokenId.Should().NotBeNullOrWhiteSpace();
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.Name.Should().Be("first");
        body.Message.Should().Be("Store this token securely. It will not be shown again.");
    }

    [Fact]
    public async Task Bootstrap_Returns403_WhenTokensAlreadyExist()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        _ = await host.TokenStore.CreateTokenAsync("existing");

        using var response = await host.Client.PostAsJsonAsync("/api/tokens/bootstrap", new { name = "first" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<TokenErrorResponse>();

        body.Should().NotBeNull();
        body!.Error.Should().Be("Bootstrap token creation is disabled because tokens already exist.");
    }

    [Fact]
    public async Task Bootstrap_StoresTokenInDatabase()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);

        using var response = await host.Client.GetAsync("/api/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenListResponse>();

        body.Should().NotBeNull();
        body!.Tokens.Should().ContainSingle(token => token.Id == bootstrapToken.TokenId &&
            token.Name == "first" &&
            !token.IsRevoked);
    }

    [Fact]
    public async Task CreateToken_RequiresAuth()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.PostAsJsonAsync("/api/tokens", new { name = "second" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateToken_ReturnsToken_WhenAuthenticated()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);

        using var response = await host.Client.PostAsJsonAsync("/api/tokens", new { name = "second" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TokenCreateResponse>();

        body.Should().NotBeNull();
        body!.TokenId.Should().NotBe(bootstrapToken.TokenId);
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.Name.Should().Be("second");

        var storedTokens = await host.TokenStore.ListTokensAsync();
        storedTokens.Should().HaveCount(2);
        storedTokens.Select(token => token.Name).Should().BeEquivalentTo(["first", "second"]);
    }

    [Fact]
    public async Task ListTokens_RequiresAuth()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.GetAsync("/api/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTokens_ReturnsAllTokens()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);
        var secondToken = await CreateTokenAsync(host, "second");

        using var response = await host.Client.GetAsync("/api/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenListResponse>();

        body.Should().NotBeNull();
        body!.Tokens.Should().HaveCount(2);
        body.Tokens.Select(token => token.Id).Should().BeEquivalentTo([bootstrapToken.TokenId, secondToken.TokenId]);
        body.Tokens.Select(token => token.Name).Should().BeEquivalentTo(["first", "second"]);
    }

    [Fact]
    public async Task RevokeToken_RequiresAuth()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.DeleteAsync("/api/tokens/missing-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeToken_MarksAsRevoked()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);
        var secondToken = await CreateTokenAsync(host, "second");

        using var revokeResponse = await host.Client.DeleteAsync($"/api/tokens/{secondToken.TokenId}");

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revokeBody = await revokeResponse.Content.ReadFromJsonAsync<TokenMessageResponse>();
        revokeBody.Should().NotBeNull();
        revokeBody!.Message.Should().Be("Token revoked.");

        using var listResponse = await host.Client.GetAsync("/api/tokens");
        var listBody = await listResponse.Content.ReadFromJsonAsync<TokenListResponse>();

        listBody.Should().NotBeNull();
        listBody!.Tokens.Should().Contain(token => token.Id == secondToken.TokenId && token.IsRevoked);
        listBody.Tokens.Should().Contain(token => token.Id == bootstrapToken.TokenId && !token.IsRevoked);
    }

    [Fact]
    public async Task RevokeToken_CannotRevokeOwnToken()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);

        using var response = await host.Client.DeleteAsync($"/api/tokens/{bootstrapToken.TokenId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<TokenErrorResponse>();

        body.Should().NotBeNull();
        body!.Error.Should().Be("Cannot revoke your current token.");
    }

    [Fact]
    public async Task SetupEndpoint_ReturnsToken_OnCompletion()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.PostAsJsonAsync("/init/api/setup", new
        {
            provider = "ollama",
            modelId = "llama3.2",
            endpoint = "http://localhost:11434",
            homePath = host.HomeDirectory,
            port = 8371,
            schedulerEnabled = true,
            telegramBotToken = string.Empty,
            whatsAppEnabled = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SetupTokenResponse>();

        body.Should().NotBeNull();
        body!.RedirectUrl.Should().Be("/");
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.TokenId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SetupToken_IsUsableForApiCalls()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var setupResponse = await CompleteSetupAsync(host);
        host.Authorize(setupResponse.Token);

        using var response = await host.Client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<OpenVEPA.Core.Sessions.Session>>();

        body.Should().NotBeNull();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokedToken_CannotAuthenticate()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);
        var secondToken = await CreateTokenAsync(host, "second");

        using var revokeResponse = await host.Client.DeleteAsync($"/api/tokens/{secondToken.TokenId}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        host.Authorize(secondToken.Token);

        using var response = await host.Client.GetAsync("/api/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bootstrap_RequiresName()
    {
        await using var host = await TokenApiTestHost.StartAsync();

        using var response = await host.Client.PostAsJsonAsync("/api/tokens/bootstrap", new { name = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<TokenErrorResponse>();

        body.Should().NotBeNull();
        body!.Error.Should().Be("Name is required.");
    }

    [Fact]
    public async Task CreateToken_RequiresName()
    {
        await using var host = await TokenApiTestHost.StartAsync();
        var bootstrapToken = await BootstrapAsync(host, "first");
        host.Authorize(bootstrapToken.Token);

        using var response = await host.Client.PostAsJsonAsync("/api/tokens", new { name = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<TokenErrorResponse>();

        body.Should().NotBeNull();
        body!.Error.Should().Be("Name is required.");
    }

    private static async Task<TokenCreateResponse> BootstrapAsync(TokenApiTestHost host, string name)
    {
        using var response = await host.Client.PostAsJsonAsync("/api/tokens/bootstrap", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TokenCreateResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<TokenCreateResponse> CreateTokenAsync(TokenApiTestHost host, string name)
    {
        using var response = await host.Client.PostAsJsonAsync("/api/tokens", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TokenCreateResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<SetupTokenResponse> CompleteSetupAsync(TokenApiTestHost host)
    {
        using var response = await host.Client.PostAsJsonAsync("/init/api/setup", new
        {
            provider = "ollama",
            modelId = "llama3.2",
            endpoint = "http://localhost:11434",
            homePath = host.HomeDirectory,
            port = 8371,
            schedulerEnabled = true,
            telegramBotToken = string.Empty,
            whatsAppEnabled = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SetupTokenResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private sealed record SetupTokenResponse(string RedirectUrl, string Token, string TokenId);
}

internal sealed class TokenApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private TokenApiTestHost(WebApplication app, HttpClient client, string homeDirectory, SqliteTokenStore tokenStore)
    {
        _app = app;
        Client = client;
        HomeDirectory = homeDirectory;
        TokenStore = tokenStore;
    }

    public HttpClient Client { get; }

    public string HomeDirectory { get; }

    public SqliteTokenStore TokenStore { get; }

    public static async Task<TokenApiTestHost> StartAsync()
    {
        var homeDirectory = Path.Combine(Path.GetTempPath(), $"openvepa-token-api-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(homeDirectory);

        var documentsPath = Path.Combine(homeDirectory, "documents");
        Directory.CreateDirectory(documentsPath);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = homeDirectory,
        });

        builder.Logging.ClearProviders();
        builder.Services.AddRouting();
        builder.Services.AddAuthentication(TokenAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(
                TokenAuthenticationHandler.SchemeName,
                static _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAgentRuntime>(new RecordingAgentRuntime());
        builder.Services.AddSingleton(new SetupCompletionService(homeDirectory));
        builder.Services.AddOpenVepaStorage($"Data Source={Path.Combine(homeDirectory, "openvepa.db")}", documentsPath);

        var app = builder.Build();
        await app.Services.InitializeStorageAsync();

        app.Urls.Add("http://127.0.0.1:0");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTokenApiEndpoints();
        app.MapSetupApiEndpoints();
        app.MapSessionApiEndpoints();

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var baseAddress = new Uri(addresses!.Addresses.First());
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseAddress,
        };

        var tokenStore = app.Services.GetRequiredService<SqliteTokenStore>();
        return new TokenApiTestHost(app, client, homeDirectory, tokenStore);
    }

    public void Authorize(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();

        try
        {
            if (Directory.Exists(HomeDirectory))
            {
                Directory.Delete(HomeDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
