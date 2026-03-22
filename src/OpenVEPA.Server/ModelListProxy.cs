using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenVEPA.Server;

/// <summary>
/// Fetches available model lists from upstream LLM provider APIs.
/// Shared between the main server setup endpoints and the standalone setup wizard.
/// </summary>
internal static class ModelListProxy
{
    /// <summary>Default API endpoints for each supported provider.</summary>
    internal static readonly Dictionary<string, string> DefaultEndpoints =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ollama"] = "http://localhost:11434",
            ["openai"] = "https://api.openai.com/v1",
            ["google"] = "https://generativelanguage.googleapis.com/v1beta",
            ["mistral"] = "https://api.mistral.ai/v1",
            ["groq"] = "https://api.groq.com/openai/v1",
            ["together"] = "https://api.together.xyz/v1",
            ["perplexity"] = "https://api.perplexity.ai",
            ["anthropic"] = "https://api.anthropic.com/v1",
            ["azure"] = "",
            ["cohere"] = "https://api.cohere.com/v2",
        };

    private static readonly HashSet<string> ListableProviders = InitListableProviders();

    private static HashSet<string> InitListableProviders()
    {
        var set = new HashSet<string>(DefaultEndpoints.Keys, StringComparer.OrdinalIgnoreCase);
        set.Remove("azure");
        return set;
    }

    /// <summary>Maximum number of models returned to prevent huge responses.</summary>
    private const int MaxModels = 50;

    /// <summary>Determines whether the given provider supports dynamic model listing.</summary>
    /// <param name="provider">The provider identifier.</param>
    /// <returns><c>true</c> if the provider exposes a model list API.</returns>
    internal static bool ProviderSupportsListing(string provider) =>
        ListableProviders.Contains(provider);

    /// <summary>Returns the default API endpoint for a provider.</summary>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The default endpoint URL, or an empty string if unknown.</returns>
    internal static string GetDefaultEndpoint(string provider) =>
        DefaultEndpoints.GetValueOrDefault(provider, string.Empty);

    /// <summary>
    /// Fetches the list of available models from the upstream provider API.
    /// </summary>
    /// <param name="provider">The provider identifier (e.g. "openai", "ollama").</param>
    /// <param name="apiKey">The API key for authentication, if required.</param>
    /// <param name="endpoint">The base endpoint URL (trailing slashes already stripped).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A sorted list of model identifiers.</returns>
    internal static async Task<List<string>> FetchModelsFromProviderAsync(
        string provider,
        string apiKey,
        string endpoint,
        CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        string url;
        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            url = $"{endpoint}/api/tags";
        }
        else if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            // Use the native Generative Language API endpoint for model listing;
            // strip any /openai suffix that may have been stored.
            var googleBase = endpoint.EndsWith("/openai", StringComparison.OrdinalIgnoreCase)
                ? endpoint[..^"/openai".Length]
                : endpoint;
            url = $"{googleBase}/models?key={Uri.EscapeDataString(apiKey)}";
        }
        else
        {
            url = $"{endpoint}/models";
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        using var response = await client.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct)
            .ConfigureAwait(false);

        var models = ExtractModelNames(doc.RootElement, provider);
        models.Sort(StringComparer.OrdinalIgnoreCase);

        return models.Count > MaxModels ? models[..MaxModels] : models;
    }

    /// <summary>
    /// Fetches models with full status and diagnostics for display in the UI.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="endpoint">The base endpoint URL.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ModelQueryResult"/> containing models or error details.</returns>
    internal static async Task<ModelQueryResult> FetchModelsWithStatusAsync(
        string provider,
        string apiKey,
        string endpoint,
        CancellationToken ct)
    {
        try
        {
            var models = await FetchModelsFromProviderAsync(provider, apiKey, endpoint, ct)
                .ConfigureAwait(false);

            var count = models.Count;
            var diagnostics = $"Connected successfully. {count} {(count == 1 ? "model" : "models")} available.";

            return new ModelQueryResult(true, models, provider, endpoint, null, diagnostics);
        }
        catch (OperationCanceledException)
        {
            return new ModelQueryResult(
                false,
                [],
                provider,
                endpoint,
                "Request was cancelled or timed out.",
                $"The request to {provider} at {endpoint} timed out. Check that the service is running and reachable.");
        }
        catch (HttpRequestException ex)
        {
            return new ModelQueryResult(
                false,
                [],
                provider,
                endpoint,
                ex.Message,
                $"Could not connect to {provider} at {endpoint}. Ensure the service is running and accessible from the Docker container.");
        }
#pragma warning disable CA1031 // Catch general exception to return structured error
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new ModelQueryResult(
                false,
                [],
                provider,
                endpoint,
                ex.Message,
                $"Unexpected error querying {provider} at {endpoint}: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts model name strings from a provider-specific JSON response.
    /// </summary>
    /// <param name="root">The root JSON element of the response.</param>
    /// <param name="provider">The provider identifier for routing parse logic.</param>
    /// <returns>A list of model name strings.</returns>
    private static List<string> ExtractModelNames(JsonElement root, string provider)
    {
        var models = new List<string>();

        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (root.TryGetProperty("models", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        models.Add(name.GetString()!);
                    }
                }
            }
        }
        else if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            if (root.TryGetProperty("models", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        var n = name.GetString()!;
                        if (n.StartsWith("models/", StringComparison.Ordinal))
                        {
                            n = n["models/".Length..];
                        }

                        if (n.Contains("gemini", StringComparison.OrdinalIgnoreCase))
                        {
                            models.Add(n);
                        }
                    }
                }
            }
        }
        else
        {
            // OpenAI-compatible: openai, mistral, groq, together, perplexity.
            if (root.TryGetProperty("data", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id))
                    {
                        var modelId = id.GetString()!;

                        if (string.Equals(provider, "openai", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!modelId.Contains("gpt", StringComparison.OrdinalIgnoreCase)
                                && !modelId.Contains("o1", StringComparison.OrdinalIgnoreCase)
                                && !modelId.Contains("o3", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        models.Add(modelId);
                    }
                }
            }
        }

        return models;
    }
}

/// <summary>Result of a model list query with success/error status and diagnostics.</summary>
/// <param name="Success">Whether the fetch succeeded.</param>
/// <param name="Models">The list of model identifiers (empty on failure).</param>
/// <param name="Provider">The provider that was queried.</param>
/// <param name="Endpoint">The endpoint that was queried.</param>
/// <param name="Error">Brief error message, or <c>null</c> on success.</param>
/// <param name="Diagnostics">Detailed diagnostic message for UI display.</param>
internal sealed record ModelQueryResult(
    bool Success,
    List<string> Models,
    string Provider,
    string Endpoint,
    string? Error,
    string Diagnostics);
