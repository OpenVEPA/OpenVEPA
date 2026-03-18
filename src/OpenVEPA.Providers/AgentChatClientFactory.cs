using System.Collections.Concurrent;
using System.Globalization;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;

namespace OpenVEPA.Providers;

/// <summary>
/// Resolves <see cref="IChatClient"/> instances per agent based on <see cref="AgentLlmConfig"/>.
/// Caches wrapped clients to avoid re-creation on every call.
/// </summary>
internal sealed class AgentChatClientFactory : IAgentChatClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ProviderOptions> _options;
    private readonly ConcurrentDictionary<string, IChatClient> _cache = new(StringComparer.Ordinal);

    public AgentChatClientFactory(
        IServiceProvider serviceProvider,
        IOptions<ProviderOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public IChatClient GetChatClient(AgentDefinition agent)
    {
        if (agent is null) throw new ArgumentNullException(nameof(agent));

        var config = agent.LlmConfig;

        if (config is null || IsEmptyConfig(config))
            return ResolveDefaultClient();

        var cacheKey = BuildCacheKey(config);
        return _cache.GetOrAdd(cacheKey, _ => CreateConfiguredClient(config));
    }

    private IChatClient ResolveDefaultClient()
    {
        return _serviceProvider.GetRequiredService<IChatClient>();
    }

    private IChatClient CreateConfiguredClient(AgentLlmConfig config)
    {
        var providerKey = config.Provider?.ToLowerInvariant()
            ?? _options.Value.DefaultProvider?.ToLowerInvariant()
            ?? "ollama";

        var baseClient = _serviceProvider.GetRequiredKeyedService<IChatClient>(providerKey);

        bool hasOverrides = config.Model is not null
            || config.Temperature is not null
            || config.MaxTokens is not null;

        if (!hasOverrides)
            return baseClient;

        return new ConfiguredChatClient(
            baseClient,
            modelId: config.Model,
            temperature: config.Temperature.HasValue ? (float)config.Temperature.Value : null,
            maxOutputTokens: config.MaxTokens);
    }

    private static bool IsEmptyConfig(AgentLlmConfig config)
    {
        return config.Provider is null
            && config.Model is null
            && config.Temperature is null
            && config.MaxTokens is null;
    }

    private static string BuildCacheKey(AgentLlmConfig config)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"{config.Provider ?? "default"}|{config.Model ?? "default"}|{config.Temperature?.ToString(CultureInfo.InvariantCulture) ?? "default"}|{config.MaxTokens?.ToString(CultureInfo.InvariantCulture) ?? "default"}");
    }
}
