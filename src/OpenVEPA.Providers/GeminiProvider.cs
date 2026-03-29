using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Providers;

/// <summary>
/// Native Google Gemini provider using the REST generateContent API.
/// Uses API key authentication via query parameter (not Bearer token).
/// </summary>
public sealed class GeminiProvider : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly ILogger? _logger;

    /// <inheritdoc/>
    public ChatClientMetadata Metadata { get; }

    private GeminiProvider(string apiKey, string model, string baseUrl, ILogger? logger)
    {
        _apiKey = apiKey;
        _model = model;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        _httpClient = new HttpClient();
        Metadata = new ChatClientMetadata("google-gemini", new Uri(_baseUrl), model);
    }

    /// <summary>Provider key used for keyed DI registration.</summary>
    public const string Key = "google";

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by the native Google Gemini REST API.
    /// </summary>
    /// <param name="apiKey">Google API key.</param>
    /// <param name="model">Default model identifier (e.g. "gemini-2.5-flash").</param>
    /// <param name="endpoint">Optional base URL. Defaults to the public Generative Language API.</param>
    /// <param name="logger">Optional logger for request/response diagnostics.</param>
    /// <returns>An <see cref="IChatClient"/> that calls the Gemini REST API.</returns>
    public static IChatClient Create(
        string apiKey,
        string model,
        string? endpoint = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Google Gemini API key is required.");

        var baseUrl = string.IsNullOrWhiteSpace(endpoint)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : endpoint.TrimEnd('/');

        // Strip /openai or /openai/ suffix if present (legacy config compat).
        if (baseUrl.EndsWith("/openai/", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/openai/".Length];
        else if (baseUrl.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/openai".Length];

        logger?.LogInformation("Creating native Gemini client: endpoint={Endpoint}, model={Model}",
            baseUrl, model);

        return new GeminiProvider(apiKey, model, baseUrl, logger);
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveModel = options?.ModelId ?? _model;
        var url = $"{_baseUrl}/models/{effectiveModel}:generateContent?key={_apiKey}";

        _logger?.LogInformation("Gemini request: model={Model}, url={Url}", effectiveModel,
            url.Replace(_apiKey, "***", StringComparison.Ordinal));

        var requestBody = BuildRequestBody(chatMessages, options);

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken)
            .ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("Gemini error: status={Status}, body={Body}",
                (int)response.StatusCode, Truncate(responseBody, 500));
            throw new HttpRequestException(
                $"Gemini API request failed. Status: {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Response: {Truncate(responseBody, 500)}",
                null, response.StatusCode);
        }

        return ParseResponse(responseBody, effectiveModel);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Fall back to non-streaming and yield as a single update.
        var response = await GetResponseAsync(chatMessages, options, cancellationToken)
            .ConfigureAwait(false);

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            ModelId = response.ModelId,
        };
        update.Contents.Add(new TextContent(response.Text));
        yield return update;
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is not null)
            return null;

        if (serviceType == typeof(ChatClientMetadata))
            return Metadata;

        if (serviceType?.IsInstanceOfType(this) == true)
            return this;

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    // ── Request building ──────────────────────────────────────────────

    private static object BuildRequestBody(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options)
    {
        var messages = chatMessages.ToList();

        // Extract system messages for the systemInstruction field.
        var systemParts = messages
            .Where(m => m.Role == ChatRole.System)
            .Select(m => m.Text)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        var contents = messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Text ?? string.Empty } },
            })
            .ToList();

        var body = new Dictionary<string, object> { ["contents"] = contents };

        if (systemParts.Count > 0)
        {
            body["systemInstruction"] = new
            {
                parts = systemParts.Select(t => new { text = t }).ToArray(),
            };
        }

        var generationConfig = BuildGenerationConfig(options);
        if (generationConfig.Count > 0)
        {
            body["generationConfig"] = generationConfig;
        }

        return body;
    }

    private static Dictionary<string, object> BuildGenerationConfig(ChatOptions? options)
    {
        var config = new Dictionary<string, object>();

        if (options?.Temperature is not null)
            config["temperature"] = options.Temperature.Value;

        if (options?.MaxOutputTokens is not null)
            config["maxOutputTokens"] = options.MaxOutputTokens.Value;

        if (options?.TopP is not null)
            config["topP"] = options.TopP.Value;

        return config;
    }

    // ── Response parsing ──────────────────────────────────────────────

    private static ChatResponse ParseResponse(string json, string modelId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Extract response text from candidates[0].content.parts[0].text.
        var text = string.Empty;
        if (root.TryGetProperty("candidates", out var candidates)
            && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.GetArrayLength() > 0
                && parts[0].TryGetProperty("text", out var textProp))
            {
                text = textProp.GetString() ?? string.Empty;
            }
        }

        var assistantMessage = new ChatMessage(ChatRole.Assistant, text);
        var chatResponse = new ChatResponse(assistantMessage) { ModelId = modelId };

        // Parse usage metadata.
        if (root.TryGetProperty("usageMetadata", out var usage))
        {
            chatResponse.Usage = new UsageDetails
            {
                InputTokenCount = usage.TryGetProperty("promptTokenCount", out var ptc) ? ptc.GetInt64() : null,
                OutputTokenCount = usage.TryGetProperty("candidatesTokenCount", out var ctc) ? ctc.GetInt64() : null,
                TotalTokenCount = usage.TryGetProperty("totalTokenCount", out var ttc) ? ttc.GetInt64() : null,
            };
        }

        return chatResponse;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
