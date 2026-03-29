using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace OpenVEPA.Server.Tests;

/// <summary>
/// Verifies that the Google Gemini native REST API endpoint URLs we construct
/// actually exist by making real HTTP requests with dummy credentials.
///
/// Key insight: a correct URL returns 400/401/403 (bad credentials) while
/// a wrong URL returns 404 (not found). No valid API key needed.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GoogleEndpointLiveTests : IDisposable
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com";
    private readonly HttpClient _http = new();

    [Fact]
    public async Task ModelListEndpoint_WithDummyKey_ReturnsNon404()
    {
        // The public model-listing endpoint accepts an API key as a query param.
        var response = await _http.GetAsync($"{BaseUrl}/v1beta/models?key=dummy_invalid_key");

        // 400 = bad key, NOT 404 = URL exists.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "the v1beta/models endpoint should exist — 400 means bad key, 404 means wrong URL");
    }

    [Fact]
    public async Task NativeGenerateContentEndpoint_WithDummyKey_ReturnsNon404()
    {
        // This is the EXACT URL the native GeminiProvider constructs:
        //   {baseUrl}/models/{model}:generateContent?key={apiKey}
        var request = BuildNativeRequest($"{BaseUrl}/v1beta/models/gemini-2.5-flash:generateContent?key=dummy_invalid_key");

        var response = await _http.SendAsync(request);

        // 400/401/403 = credentials rejected, but endpoint exists.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "v1beta/models/gemini-2.5-flash:generateContent should exist — a non-404 proves the URL is correct");
    }

    [Fact]
    public async Task WrongEndpoint_WithOpenAiChatCompletions_IsNotNativeApi()
    {
        // The OLD OpenAI-compat URL: /v1beta/openai/chat/completions
        // This may still exist on Google's side but is NOT what the native provider uses.
        var request = BuildOpenAiCompatRequest($"{BaseUrl}/v1beta/openai/chat/completions");

        var response = await _http.SendAsync(request);

        // We don't assert 404 because Google may still route this, but confirm it's
        // a different path than the native API endpoint.
        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "the OpenAI-compat path should not succeed with a dummy key");
    }

    [Fact]
    public void EmptyUriString_ThrowsUriFormatException()
    {
        // Guard: constructing a URI from an empty string must fail fast.
        var act = () => new Uri("");

        act.Should().Throw<UriFormatException>();
    }

    public void Dispose() => _http.Dispose();

    private static HttpRequestMessage BuildNativeRequest(string url)
    {
        // Native Gemini REST API request body format.
        var body = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = "ping" } },
                },
            },
        });

        return new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpRequestMessage BuildOpenAiCompatRequest(string url)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = "gemini-2.5-flash",
            messages = new[] { new { role = "user", content = "ping" } },
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "dummy_invalid_key");
        return request;
    }
}
