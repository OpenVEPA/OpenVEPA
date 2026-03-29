using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Audit;

namespace OpenVEPA.Providers;

/// <summary>
/// Extension methods for registering LLM provider services in the DI container.
/// </summary>
public static class ProviderServiceExtensions
{
    /// <summary>
    /// Registers OpenVEPA LLM providers based on the "Providers" configuration section.
    /// Registers keyed <see cref="IChatClient"/> services for each configured provider
    /// and a non-keyed default based on <see cref="ProviderOptions.DefaultProvider"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration root used to bind <see cref="ProviderOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenVepaProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<ProviderOptions>(configuration.GetSection("Providers"));

        RegisterOpenAi(services);
        RegisterOllama(services);
        RegisterGoogle(services);
        RegisterDefault(services);

        services.AddSingleton<IAgentChatClientFactory, AgentChatClientFactory>();

        return services;
    }

    private static void RegisterOpenAi(IServiceCollection services)
    {
        services.AddKeyedSingleton<IChatClient>(OpenAiProvider.Key, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var openAiOptions = options.OpenAi;

            if (openAiOptions is null || string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI provider requested but no API key is configured. " +
                    "Set Providers:OpenAi:ApiKey in configuration.");
            }

            var auditLogger = ResolveAuditLogger(sp);
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("OpenVEPA.Providers.OpenAiProvider");
            return OpenAiProvider.Create(openAiOptions, auditLogger, logger);
        });
    }

    private static void RegisterOllama(IServiceCollection services)
    {
        services.AddKeyedSingleton<IChatClient>(OllamaProvider.Key, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var ollamaOptions = options.Ollama ?? new OllamaOptions();

            var auditLogger = ResolveAuditLogger(sp);
            return OllamaProvider.Create(ollamaOptions, auditLogger);
        });
    }

    /// <summary>Provider key used for the Google Gemini keyed DI registration.</summary>
    private const string GoogleKey = "google";

    private static void RegisterGoogle(IServiceCollection services)
    {
        services.AddKeyedSingleton<IChatClient>(GoogleKey, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;

            // Look for a Google instance in the multi-instance list.
            var instance = options.Instances?.Find(
                i => string.Equals(i.Type, "google", StringComparison.OrdinalIgnoreCase));

            if (instance is null || string.IsNullOrWhiteSpace(instance.ApiKey))
            {
                throw new InvalidOperationException(
                    "Google provider requested but no instance with type 'google' and an API key is configured.");
            }

            var auditLogger = ResolveAuditLogger(sp);
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("OpenVEPA.Providers.GeminiProvider");
            return CreateGoogleClient(instance, model: null, auditLogger, logger);
        });
    }

    private static void RegisterDefault(IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var defaultKey = options.DefaultProvider?.ToLowerInvariant() ?? "ollama";

            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("OpenVEPA.Providers.ProviderServiceExtensions");
            logger?.LogInformation("Registering default provider: {Type}", defaultKey);

            // Try instance-based config first (new multi-instance format).
            var instance = options.Instances?.Find(
                i => string.Equals(i.Name, defaultKey, StringComparison.OrdinalIgnoreCase));

            if (instance is not null)
            {
                var auditLogger = ResolveAuditLogger(sp);
                return CreateFromInstance(instance, model: null, auditLogger, logger);
            }

            // Fall back to keyed service (legacy section-based format).
            return sp.GetRequiredKeyedService<IChatClient>(defaultKey);
        });
    }

    /// <summary>
    /// Creates an <see cref="IChatClient"/> for the given provider instance and optional model override.
    /// </summary>
    public static IChatClient CreateFromInstance(
        ProviderInstanceOptions instance,
        string? model,
        Func<LlmAuditLogEntry, Task>? auditLogger,
        ILogger? logger = null)
    {
#pragma warning disable CS0618 // Obsolete ModelId -> DefaultModel compat
        var effectiveModel = model ?? instance.DefaultModel;
#pragma warning restore CS0618
        var type = instance.Type?.ToLowerInvariant() ?? "ollama";

        logger?.LogInformation("Creating {ProviderType} client: endpoint={Endpoint}, model={Model}",
            type, instance.Endpoint ?? "(default)", effectiveModel ?? "(default)");

        if (string.Equals(type, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            return OllamaProvider.Create(new OllamaOptions
            {
                Endpoint = string.IsNullOrWhiteSpace(instance.Endpoint) ? "http://localhost:11434" : instance.Endpoint,
                Model = effectiveModel ?? "llama3.2",
            }, auditLogger);
        }

        if (string.Equals(type, "google", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(instance.ApiKey))
            {
                logger?.LogError("API key is missing for Google provider '{InstanceName}'", instance.Name);
                throw new InvalidOperationException(
                    $"API key is missing for Google provider '{instance.Name}'. " +
                    "Please configure the API key in Settings → LLM Providers.");
            }

            return CreateGoogleClient(instance, effectiveModel, auditLogger, logger);
        }

        // All other providers (openai, anthropic, mistral, groq, openrouter, together,
        // perplexity, etc.) use the OpenAI-compatible SDK with their own endpoint.
        if (string.IsNullOrWhiteSpace(instance.ApiKey))
        {
            logger?.LogError("API key is missing for {ProviderType} provider '{InstanceName}'", type, instance.Name);
            throw new InvalidOperationException(
                $"API key is missing for {type} provider '{instance.Name}'. " +
                "Please configure the API key in Settings → LLM Providers.");
        }

        return OpenAiProvider.Create(new OpenAiOptions
        {
            ApiKey = instance.ApiKey,
            Model = effectiveModel ?? "gpt-4o",
            Endpoint = string.IsNullOrWhiteSpace(instance.Endpoint) ? null : instance.Endpoint,
            Provider = instance.Type?.ToLowerInvariant() ?? "openai",
        }, auditLogger, logger);
    }

    /// <summary>Default base endpoint for the Google Generative Language API.</summary>
    internal const string GoogleDefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";

    private static IChatClient CreateGoogleClient(
        ProviderInstanceOptions instance,
        string? model,
        Func<LlmAuditLogEntry, Task>? auditLogger,
        ILogger? logger = null)
    {
#pragma warning disable CS0618
        var effectiveModel = model ?? instance.DefaultModel ?? "gemini-2.5-flash";
#pragma warning restore CS0618

        if (string.IsNullOrWhiteSpace(instance.ApiKey))
        {
            logger?.LogError("API key is missing for Google provider '{InstanceName}'", instance.Name);
            throw new InvalidOperationException(
                $"API key is missing for Google provider '{instance.Name}'. " +
                "Please configure the API key in Settings → LLM Providers.");
        }

        IChatClient innerClient = GeminiProvider.Create(
            instance.ApiKey,
            effectiveModel,
            instance.Endpoint,
            logger);

        return new TokenTrackingChatClient(innerClient, "google", effectiveModel, auditLogger);
    }

    private static Func<LlmAuditLogEntry, Task>? ResolveAuditLogger(IServiceProvider sp)
    {
        return sp.GetService<Func<LlmAuditLogEntry, Task>>();
    }
}
