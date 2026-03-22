using System.Collections.Concurrent;
using System.Globalization;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Audit;

namespace OpenVEPA.Providers;

/// <summary>
/// Resolves <see cref="IChatClient"/> instances per agent based on <see cref="AgentLlmConfig"/>.
/// Caches wrapped clients to avoid re-creation on every call.
/// When provider configuration changes at runtime the cache is automatically cleared
/// so subsequent requests pick up the updated settings without an application restart.
/// </summary>
internal sealed class AgentChatClientFactory : IAgentChatClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ProviderOptions> _optionsMonitor;
    private readonly ILogger<AgentChatClientFactory> _logger;
    private readonly Func<LlmAuditLogEntry, Task>? _auditLogger;
    private readonly ConcurrentDictionary<string, IChatClient> _cache = new(StringComparer.Ordinal);
    private bool _loggedOptionsOnce;

    public AgentChatClientFactory(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ProviderOptions> optionsMonitor,
        ILogger<AgentChatClientFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogger = serviceProvider.GetService<Func<LlmAuditLogEntry, Task>>();

        _optionsMonitor.OnChange(_ =>
        {
            _logger.LogInformation("Provider configuration changed, clearing client cache");
            _cache.Clear();
        });
    }

    /// <inheritdoc/>
    public IChatClient GetChatClient(AgentDefinition agent)
    {
        if (agent is null) throw new ArgumentNullException(nameof(agent));

        var config = agent.LlmConfig;

        if (config is null || IsEmptyConfig(config))
        {
            _logger.LogDebug("Agent {AgentName} has empty LLM config, using default", agent.Name);
            return ResolveDefaultClient();
        }

        var cacheKey = BuildCacheKey(config);

        _logger.LogInformation("Resolving chat client for agent {AgentName}: provider={Provider}, model={Model}",
            agent.Name, config.Provider ?? "default", config.Model ?? "default");

        if (_cache.TryGetValue(cacheKey, out _))
            _logger.LogDebug("Cache hit for key={CacheKey}", cacheKey);

        return _cache.GetOrAdd(cacheKey, _ => CreateConfiguredClient(config));
    }

    private IChatClient ResolveDefaultClient()
    {
        if (!_loggedOptionsOnce)
        {
            _loggedOptionsOnce = true;
            var currentOptions = _optionsMonitor.CurrentValue;
            _logger.LogInformation(
                "ProviderOptions state: DefaultProvider={DefaultProvider}, InstanceCount={Count}, InstanceNames={Names}",
                currentOptions.DefaultProvider,
                currentOptions.Instances?.Count ?? 0,
                currentOptions.Instances is not null
                    ? string.Join(", ", currentOptions.Instances.Select(i => $"{i.Name}({i.Type})"))
                    : "(none)");
        }

        return _cache.GetOrAdd("__default__", _ =>
        {
            var options = _optionsMonitor.CurrentValue;
            var defaultKey = options.DefaultProvider?.ToLowerInvariant() ?? "ollama";
            _logger.LogInformation("Resolving default provider '{DefaultProvider}' (configured default: '{ConfiguredDefault}')",
                defaultKey, options.DefaultProvider ?? "(none, falling back to ollama)");
            return CreateClientForInstance(defaultKey, model: null, options);
        });
    }

    private IChatClient CreateConfiguredClient(AgentLlmConfig config)
    {
        var options = _optionsMonitor.CurrentValue;
        var providerKey = config.Provider?.ToLowerInvariant()
            ?? options.DefaultProvider?.ToLowerInvariant()
            ?? "ollama";

        var baseClient = CreateClientForInstance(providerKey, config.Model, options);

        bool hasOverrides = config.Temperature is not null
            || config.MaxTokens is not null;

        if (!hasOverrides)
            return baseClient;

        return new ConfiguredChatClient(
            baseClient,
            modelId: null,
            temperature: config.Temperature.HasValue ? (float)config.Temperature.Value : null,
            maxOutputTokens: config.MaxTokens);
    }

    private IChatClient CreateClientForInstance(string instanceName, string? model, ProviderOptions options)
    {
        if (options.Instances is null or { Count: 0 })
        {
            _logger.LogError("No provider instances configured. Cannot resolve '{InstanceName}'", instanceName);
            throw new InvalidOperationException(
                "No LLM providers are configured. Please add a provider in Settings → LLM Providers.");
        }

        // Try instance-based config first (multi-instance format).
        var instance = options.Instances.Find(
            i => string.Equals(i.Name, instanceName, StringComparison.OrdinalIgnoreCase));

        if (instance is null)
        {
            _logger.LogDebug("Instance '{InstanceName}' not found by name, trying by type", instanceName);
            // Fallback: try by type (deprecated).
            instance = options.Instances.Find(
                i => string.Equals(i.Type, instanceName, StringComparison.OrdinalIgnoreCase));
        }

        if (instance is null)
        {
            var available = string.Join(", ", options.Instances.Select(i => i.Name));
            _logger.LogError("Provider instance '{InstanceName}' not found. Available: {Available}",
                instanceName, available);
            throw new InvalidOperationException(
                $"LLM provider '{instanceName}' is not configured. " +
                $"Available providers: {(string.IsNullOrEmpty(available) ? "(none)" : available)}. " +
                "Please check your provider configuration in Settings → LLM Providers.");
        }

#pragma warning disable CS0618 // Obsolete ModelId -> DefaultModel compat
        var effectiveModel = model ?? instance.DefaultModel;
#pragma warning restore CS0618

        _logger.LogInformation("Creating client for instance='{InstanceName}', model={EffectiveModel}",
            instanceName, effectiveModel ?? "default");
        return ProviderServiceExtensions.CreateFromInstance(instance, effectiveModel, _auditLogger, _logger);
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
