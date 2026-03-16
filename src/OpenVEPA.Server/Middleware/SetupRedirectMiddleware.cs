using Microsoft.AspNetCore.Http;

namespace OpenVEPA.Server.Middleware;

/// <summary>
/// Redirects all requests to the setup wizard when initial setup has not been completed.
/// Requests to /init/* paths are allowed through unconditionally.
/// Also allows /health for Docker health checks during setup.
/// </summary>
internal sealed class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SetupCompletionService _setupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupRedirectMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="setupService">Service that tracks setup completion state.</param>
    public SetupRedirectMiddleware(RequestDelegate next, SetupCompletionService setupService)
    {
        _next = next;
        _setupService = setupService;
    }

    /// <summary>Processes the HTTP request, redirecting to the setup wizard if setup is incomplete.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (_setupService.IsSetupComplete)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Allow setup wizard routes and health check through.
        if (path.StartsWith("/init/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/init", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Redirect everything else to the setup wizard.
        context.Response.Redirect("/init/setupwizard");
    }
}
