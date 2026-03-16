using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVEPA.Server.Auth;
using OpenVEPA.Server.Health;

namespace OpenVEPA.Server;

/// <summary>Extension methods for registering OpenVEPA server services.</summary>
public static class ServerServiceExtensions
{
    /// <summary>
    /// Registers SignalR, authentication, health checks, and CORS
    /// for the OpenVEPA server.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="openVepaHome">The OpenVEPA home directory path.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenVepaServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string openVepaHome)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        // Setup completion tracking.
        services.AddSingleton(new SetupCompletionService(openVepaHome));

        // SignalR with JSON protocol.
        services.AddSignalR();

        // Custom token authentication.
        services
            .AddAuthentication(TokenAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(
                TokenAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization();

        // Loopback token for local TUI access.
        services.AddSingleton<LoopbackTokenService>();
        services.AddHostedService(sp => sp.GetRequiredService<LoopbackTokenService>());

        // Health checks.
        services.AddHealthChecks()
            .AddCheck<OpenVepaHealthCheck>("database");

        // CORS (permissive for development).
        // AllowAnyOrigin and AllowCredentials are mutually exclusive in ASP.NET Core.
        // When no origins are configured, allow localhost by default.
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = configuration.GetSection("OpenVEPA:Cors:Origins").Get<string[]>();
                if (origins is { Length: > 0 })
                {
                    policy.WithOrigins(origins);
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true);
                }

                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
