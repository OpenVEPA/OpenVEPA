using System.ClientModel;
using System.ClientModel.Primitives;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    /// <param name="logger">Optional logger for HTTP request/response diagnostics.</param>
    /// <returns>A token-tracking chat client wrapping the OpenAI client.</returns>
    public static IChatClient Create(
        OpenAiOptions options,
        Func<LlmAuditLogEntry, Task>? auditLogger = null,
        ILogger? logger = null)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI API key is required.");

        var credential = new ApiKeyCredential(options.ApiKey);

        OpenAIClientOptions clientOptions = new();
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException(
                    $"Invalid endpoint URL '{options.Endpoint}' for OpenAI provider. " +
                    "The endpoint must be a valid absolute URL (e.g., https://api.openai.com/v1).");
            }

            clientOptions.Endpoint = endpointUri;
        }

        if (logger is not null)
        {
            var httpClient = new HttpClient(new LoggingHttpHandler(logger) { InnerHandler = new HttpClientHandler() });
            clientOptions.Transport = new HttpClientPipelineTransport(httpClient);
        }

        var openAiClient = new OpenAIClient(credential, clientOptions);
        IChatClient innerClient = openAiClient.GetChatClient(options.Model).AsIChatClient();

        return new TokenTrackingChatClient(innerClient, options.Provider, options.Model, auditLogger);
    }
}
