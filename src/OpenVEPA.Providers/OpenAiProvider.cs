using System.ClientModel;

using Microsoft.Extensions.AI;

using OpenAI;

using OpenVEPA.Core.Audit;

namespace OpenVEPA.Providers;

/// <summary>
/// Factory for creating an OpenAI-backed <see cref="IChatClient"/>
/// wrapped with token tracking.
/// </summary>
public static class OpenAiProvider
{
    /// <summary>Provider key used for keyed DI registration.</summary>
    public const string Key = "openai";

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by OpenAI.
    /// </summary>
    /// <param name="options">OpenAI configuration (API key, model, optional endpoint).</param>
    /// <param name="auditLogger">Optional audit callback for token tracking.</param>
    /// <returns>A token-tracking chat client wrapping the OpenAI client.</returns>
    public static IChatClient Create(
        OpenAiOptions options,
        Func<LlmAuditLogEntry, Task>? auditLogger = null)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI API key is required.");

        var credential = new ApiKeyCredential(options.ApiKey);

        OpenAIClientOptions clientOptions = new();
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            clientOptions.Endpoint = new Uri(options.Endpoint);
        }

        var openAiClient = new OpenAIClient(credential, clientOptions);
        IChatClient innerClient = openAiClient.GetChatClient(options.Model).AsIChatClient();

        return new TokenTrackingChatClient(innerClient, Key, options.Model, auditLogger);
    }
}
