using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

namespace OpenVEPA.Providers;

/// <summary>
/// Delegating <see cref="IChatClient"/> that applies default <see cref="ChatOptions"/>
/// for agent-specific settings. Agent-level values act as defaults; caller-provided
/// options take precedence.
/// </summary>
internal sealed class ConfiguredChatClient : DelegatingChatClient
{
    private readonly string? _modelId;
    private readonly float? _temperature;
    private readonly int? _maxOutputTokens;

    /// <summary>Creates a new configured wrapper around <paramref name="innerClient"/>.</summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="modelId">Default model identifier, or null to leave unchanged.</param>
    /// <param name="temperature">Default sampling temperature, or null to leave unchanged.</param>
    /// <param name="maxOutputTokens">Default max output tokens, or null to leave unchanged.</param>
    public ConfiguredChatClient(
        IChatClient innerClient,
        string? modelId,
        float? temperature,
        int? maxOutputTokens)
        : base(innerClient)
    {
        _modelId = modelId;
        _temperature = temperature;
        _maxOutputTokens = maxOutputTokens;
    }

    /// <inheritdoc/>
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return base.GetResponseAsync(chatMessages, MergeOptions(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return base.GetStreamingResponseAsync(chatMessages, MergeOptions(options), cancellationToken);
    }

    private ChatOptions? MergeOptions(ChatOptions? callerOptions)
    {
        if (_modelId is null && _temperature is null && _maxOutputTokens is null)
            return callerOptions;

        var merged = callerOptions is not null
            ? callerOptions.Clone()
            : new ChatOptions();

        merged.ModelId ??= _modelId;
        merged.Temperature ??= _temperature;
        merged.MaxOutputTokens ??= _maxOutputTokens;

        return merged;
    }
}
