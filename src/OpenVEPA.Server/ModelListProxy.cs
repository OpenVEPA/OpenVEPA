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
        };

    private static readonly HashSet<string> ListableProviders =
        new(DefaultEndpoints.Keys, StringComparer.OrdinalIgnoreCase);

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
            url = $"{endpoint}/models?key={Uri.EscapeDataString(apiKey)}";
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
