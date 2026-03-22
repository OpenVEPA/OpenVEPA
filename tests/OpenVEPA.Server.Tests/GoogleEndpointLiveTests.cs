using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace OpenVEPA.Server.Tests;

/// <summary>
/// Verifies that the Google Gemini OpenAI-compatible endpoint URLs we construct
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
    public async Task ChatCompletionsEndpoint_WithDummyBearer_ReturnsNon404()
    {
        // This is the EXACT URL the openai-dotnet SDK constructs:
        //   base = "https://generativelanguage.googleapis.com/v1beta/openai"
        //   SDK appends "/chat/completions"
        var request = BuildChatRequest($"{BaseUrl}/v1beta/openai/chat/completions");

        var response = await _http.SendAsync(request);

        // 400/401/403 = credentials rejected, but endpoint exists.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "v1beta/openai/chat/completions should exist — a non-404 proves the URL is correct");
    }

    [Fact]
    public async Task WrongEndpointWithDoubleV1_IsRejected()
    {
        // The OLD buggy URL that doubled the path segment:
        //   base = "https://generativelanguage.googleapis.com/v1beta/openai"
        //   + SDK default "/v1" + "/chat/completions"
        // Google routes this path but rejects it (400 Bad Request),
        // confirming the extra /v1 segment is incorrect.
        var request = BuildChatRequest($"{BaseUrl}/v1beta/openai/v1/chat/completions");

        var response = await _http.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "the doubled /v1 path must not succeed — Google rejects it even though it routes");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public void EmptyUriString_ThrowsUriFormatException()
    {
        // Guard: constructing a URI from an empty string must fail fast.
        var act = () => new Uri("");

        act.Should().Throw<UriFormatException>();
    }

    public void Dispose() => _http.Dispose();

    private static HttpRequestMessage BuildChatRequest(string url)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = "gemini-2.0-flash",
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
