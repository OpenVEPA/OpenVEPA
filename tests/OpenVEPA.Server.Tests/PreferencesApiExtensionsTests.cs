using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Preferences;
using OpenVEPA.Server.Api;

namespace OpenVEPA.Server.Tests;

/// <summary>Integration tests for user preference REST API endpoints.</summary>
public sealed class PreferencesApiExtensionsTests
{
    [Fact]
    public async Task GetPreferences_ReturnsUnauthorized_WhenAuthenticationIsMissing()
    {
        await using var host = await PreferencesApiTestHost.StartAsync().ConfigureAwait(true);

        var response = await host.Client.GetAsync("/api/preferences").ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferences_ReturnsEntriesGroupedByCategory()
    {
        await using var host = await PreferencesApiTestHost.StartAsync().ConfigureAwait(true);
        host.Authorize();

        await host.PreferenceService.SetExplicitPreferenceAsync(
                "pref.name",
                "John",
                "personal",
                CancellationToken.None)
            .ConfigureAwait(true);
        await host.PreferenceService.SetExplicitPreferenceAsync(
                "ui.theme",
                "dark",
                "ui",
                CancellationToken.None)
            .ConfigureAwait(true);

        var response = await host.Client.GetAsync("/api/preferences").ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<Dictionary<string, Dictionary<string, PreferenceApiEntryDto>>>()
            .ConfigureAwait(true);

        body.Should().NotBeNull();
        body!.Keys.Should().BeEquivalentTo(["personal", "ui"]);
        body["personal"]["pref.name"].Value.Should().Be("John");
        body["ui"]["ui.theme"].Value.Should().Be("dark");
    }

    [Fact]
    public async Task PreferenceCrudEndpoints_CreateUpdateReadAndDeletePreferences()
    {
        await using var host = await PreferencesApiTestHost.StartAsync().ConfigureAwait(true);
        host.Authorize();

        var createResponse = await host.Client.PostAsJsonAsync(
                "/api/preferences",
                new { key = "pref.name", value = "John", category = "personal" })
            .ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();
        createResponse.Headers.Location!.ToString().Should().Be("/api/preferences/pref.name");

        var updateResponse = await host.Client.PutAsJsonAsync(
                "/api/preferences/pref.name",
                new { value = "Jane", category = "personal" })
            .ConfigureAwait(true);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getByCategoryResponse = await host.Client.GetAsync("/api/preferences/personal").ConfigureAwait(true);

        getByCategoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryBody = await getByCategoryResponse.Content
            .ReadFromJsonAsync<Dictionary<string, PreferenceApiEntryDto>>()
            .ConfigureAwait(true);

        categoryBody.Should().NotBeNull();
        categoryBody!.Should().ContainKey("pref.name");
        categoryBody["pref.name"].Value.Should().Be("Jane");
        categoryBody["pref.name"].Category.Should().Be("personal");

        var deleteResponse = await host.Client.DeleteAsync("/api/preferences/pref.name").ConfigureAwait(true);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedBody = await (await host.Client.GetAsync("/api/preferences/personal").ConfigureAwait(true))
            .Content
            .ReadFromJsonAsync<Dictionary<string, PreferenceApiEntryDto>>()
            .ConfigureAwait(true);

        deletedBody.Should().NotBeNull();
        deletedBody.Should().BeEmpty();
    }

    private sealed class PreferenceApiEntryDto
    {
        public string Key { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public PreferenceSource Source { get; init; }

        public double Confidence { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    private sealed class PreferencesApiTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private PreferencesApiTestHost(
            WebApplication app,
            HttpClient client,
            InMemoryUserProfileService preferenceService)
        {
            _app = app;
            Client = client;
            PreferenceService = preferenceService;
        }

        public HttpClient Client { get; }

        public InMemoryUserProfileService PreferenceService { get; }

        public static async Task<PreferencesApiTestHost> StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    static _ => { });

            var preferenceService = new InMemoryUserProfileService();
            builder.Services.AddSingleton<IUserProfileService>(preferenceService);

            var app = builder.Build();
            app.Urls.Add("http://127.0.0.1:0");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapPreferencesApiEndpoints();

            await app.StartAsync().ConfigureAwait(true);

            var server = app.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>();
            var baseAddress = new Uri(addresses!.Addresses.First());
            var client = new HttpClient { BaseAddress = baseAddress };

            return new PreferencesApiTestHost(app, client, preferenceService);
        }

        public void Authorize()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                TestAuthenticationHandler.SchemeName);
        }

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

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!string.Equals(
                    Request.Headers.Authorization.ToString(),
                    SchemeName,
                    StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Name, "test-user")
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class InMemoryUserProfileService : IUserProfileService
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, PreferenceEntry> _entries = new(StringComparer.Ordinal);

        public Task<UserPreferences> GetRelevantPreferencesAsync(string? taskDomain, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                var entries = _entries.Values
                    .Where(preference => taskDomain is null || string.Equals(
                        preference.Category,
                        taskDomain,
                        StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(preference => preference.Key, preference => preference, StringComparer.Ordinal);

                return Task.FromResult(new UserPreferences(entries));
            }
        }

        public Task SetExplicitPreferenceAsync(
            string key,
            string value,
            string category,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                _entries[key] = new PreferenceEntry(
                    key,
                    value,
                    category,
                    PreferenceSource.Explicit,
                    1.0,
                    DateTime.UtcNow);
            }

            return Task.CompletedTask;
        }

        public Task DeletePreferenceAsync(string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                _entries.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PreferenceEntry>> GetAllPreferencesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                IReadOnlyList<PreferenceEntry> preferences = _entries.Values
                    .OrderBy(preference => preference.Category, StringComparer.Ordinal)
                    .ThenBy(preference => preference.Key, StringComparer.Ordinal)
                    .ToArray();

                return Task.FromResult(preferences);
            }
        }
    }
}

