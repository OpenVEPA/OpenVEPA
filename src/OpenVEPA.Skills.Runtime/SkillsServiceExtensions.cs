using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Extension methods for registering skills runtime services in the DI container.
/// </summary>
public static class SkillsServiceExtensions
{
    /// <summary>
    /// Registers the OpenVEPA skills runtime, including directory scanning, manifest parsing,
    /// native skill loading, and the <see cref="ISkillRuntime"/> singleton.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration root used to bind <see cref="SkillsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenVepaSkills(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<SkillsOptions>(configuration.GetSection("Skills"));

        services.AddSingleton<SkillMdParser>();
        services.AddSingleton<SkillDirectory>();
        services.AddSingleton<NativeSkillLoader>();
        services.AddSingleton<ISkillRuntime, SkillRuntime>();

        return services;
    }
}
