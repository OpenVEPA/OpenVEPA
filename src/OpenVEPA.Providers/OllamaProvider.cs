using Microsoft.Extensions.AI;

using OllamaSharp;

using OpenVEPA.Core.Audit;

namespace OpenVEPA.Providers;

/// <summary>
/// Factory for creating an Ollama-backed <see cref="IChatClient"/>
/// wrapped with token tracking.
/// </summary>
public static class OllamaProvider
{
    /// <summary>Provider key used for keyed DI registration.</summary>
    public const string Key = "ollama";

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by Ollama.
    /// </summary>
    /// <param name="options">Ollama configuration (endpoint, model).</param>
    /// <param name="auditLogger">Optional audit callback for token tracking.</param>
    /// <returns>A token-tracking chat client wrapping the Ollama client.</returns>
    public static IChatClient Create(
        OllamaOptions options,
        Func<LlmAuditLogEntry, Task>? auditLogger = null)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        var uri = new Uri(options.Endpoint);
        var ollamaClient = new OllamaApiClient(uri, options.Model);

        // OllamaApiClient explicitly implements IChatClient.
        IChatClient innerClient = ollamaClient;

        return new TokenTrackingChatClient(innerClient, Key, options.Model, auditLogger);
    }
}
