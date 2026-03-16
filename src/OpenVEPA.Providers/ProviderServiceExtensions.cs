using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

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
        RegisterDefault(services);

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
            return OpenAiProvider.Create(openAiOptions, auditLogger);
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

    private static void RegisterDefault(IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var defaultKey = options.DefaultProvider?.ToLowerInvariant() ?? "ollama";

            return sp.GetRequiredKeyedService<IChatClient>(defaultKey);
        });
    }

    private static Func<LlmAuditLogEntry, Task>? ResolveAuditLogger(IServiceProvider sp)
    {
        return sp.GetService<Func<LlmAuditLogEntry, Task>>();
    }
}
