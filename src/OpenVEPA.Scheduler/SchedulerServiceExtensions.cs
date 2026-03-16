using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenVEPA.Scheduler;

/// <summary>Extension methods for registering OpenVEPA scheduler services.</summary>
public static class SchedulerServiceExtensions
{
    /// <summary>
    /// Registers Hangfire, scheduler service, task dispatcher, and configuration
    /// bindings for the OpenVEPA task scheduler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenVepaScheduler(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        // Bind options from configuration.
        services.Configure<SchedulerOptions>(
            configuration.GetSection("Scheduler"));
        services.Configure<ScheduleConfiguration>(
            configuration.GetSection("Scheduler"));

        // Register Hangfire with in-memory storage for Phase 1.
        // Task durability is provided by our own SQLite TaskItems table.
        services.AddHangfire(config =>
        {
            config.UseInMemoryStorage();
        });

        // Configure the Hangfire background job server.
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = ["default"];
            options.ServerName = "OpenVEPA.Scheduler";
        });

        // Register scheduler components.
        services.AddSingleton<SchedulerService>();
        services.AddSingleton<TaskDispatcher>();

        return services;
    }
}
