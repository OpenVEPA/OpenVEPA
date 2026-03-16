using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenVEPA.Scheduler;

/// <summary>Extension methods for mapping scheduler endpoints on a WebApplication.</summary>
public static class SchedulerEndpointExtensions
{
    /// <summary>
    /// Configures the Hangfire dashboard (if enabled) and registers
    /// configured recurring schedules on application startup.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapSchedulerEndpoints(this WebApplication app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        var options = app.Services
            .GetRequiredService<IOptions<SchedulerOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return app;
        }

        // Optionally map the Hangfire dashboard (localhost only by default).
        if (options.DashboardEnabled && !string.IsNullOrEmpty(options.DashboardPath))
        {
            app.UseHangfireDashboard(options.DashboardPath);
        }

        // Register all configured recurring schedules.
        var scheduler = app.Services.GetRequiredService<SchedulerService>();
        scheduler.RegisterConfiguredSchedules();

        return app;
    }
}
