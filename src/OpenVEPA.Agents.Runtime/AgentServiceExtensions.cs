using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenVEPA.Core.Agents;

namespace OpenVEPA.Agents.Runtime;

/// <summary>Extension methods for registering agent runtime services.</summary>
public static class AgentServiceExtensions
{
    /// <summary>Adds the OpenVEPA agent runtime services to the service collection.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration for binding agent options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenVepaAgents(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<AgentOptions>(configuration.GetSection("OpenVEPA:Agents"));

        services.AddSingleton<AgentMdParser>();
        services.AddSingleton<AgentDirectory>();
        services.AddSingleton<AssistantAgent>();
        services.AddSingleton<IAgentRuntime, AgentRuntime>();

        return services;
    }
}
