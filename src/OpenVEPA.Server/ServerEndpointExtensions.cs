using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using OpenVEPA.Server.Api;
using OpenVEPA.Server.Auth;
using OpenVEPA.Server.Hubs;
using OpenVEPA.Server.Middleware;
using OpenVEPA.Storage;

namespace OpenVEPA.Server;

/// <summary>Extension methods for mapping OpenVEPA server endpoints.</summary>
public static class ServerEndpointExtensions
{
    /// <summary>
    /// Maps the SignalR hub, health check, and token validation endpoints.
    /// Also adds request logging middleware and first-run setup redirect.
    /// </summary>
    public static WebApplication MapOpenVepaEndpoints(this WebApplication app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        // Setup redirect middleware — must be BEFORE auth so unauthenticated
        // users are redirected to the wizard on first run.
        app.UseMiddleware<SetupRedirectMiddleware>();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Setup wizard API endpoints (no auth required).
        app.MapSetupApiEndpoints();

        // SignalR hub for assistant chat.
        app.MapHub<AssistantHub>("/hub/assistant");

        // Health check endpoint.
        app.MapHealthChecks("/health");

        // Dashboard page (post-setup).
        app.MapGet("/", () => Results.Content(DashboardPageHtml.Content, "text/html"));

        // Lightweight status endpoint consumed by the dashboard.
        app.MapGet("/api/status", (SetupCompletionService setupService,
            IConfiguration configuration) =>
        {
            var provider = configuration["Providers:DefaultProvider"] ?? "unknown";
            var model = configuration["Providers:Ollama:ModelId"]
                        ?? configuration["Providers:OpenAi:ModelId"]
                        ?? "unknown";

            return Results.Ok(new
            {
                status = "running",
                version = "1.0.0",
                provider,
                model,
                setupComplete = setupService.IsSetupComplete,
            });
        });

        // Token validation endpoint (for clients to verify their token).
        app.MapGet("/api/token/validate", async (
            HttpContext context,
            SqliteTokenStore tokenStore) =>
        {
            var token = ExtractBearerToken(context);
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var tokenId = await tokenStore
                .ValidateTokenAsync(token, context.RequestAborted)
                .ConfigureAwait(false);

            return tokenId is not null
                ? Results.Ok(new { valid = true, tokenId })
                : Results.Unauthorized();
        }).RequireAuthorization();

        app.MapSessionApiEndpoints();
        app.MapAgentsApiEndpoints();
        app.MapSkillsApiEndpoints();
        app.MapPreferencesApiEndpoints();
        app.MapTokenApiEndpoints();
        app.MapSystemApiEndpoints();
        app.MapChannelsApiEndpoints();

        return app;
    }

    private static string? ExtractBearerToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization) &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        return null;
    }
}
