using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for orchestrator configuration.</summary>
internal static class OrchestratorApiExtensions
{
    private const string ConfigCategory = "orchestrator-config";
    private const string VisibilityKey = "orchestrator:delegation-visibility";

    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/orchestrator</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapOrchestratorApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder group = app.MapGroup("/api/orchestrator")
            .RequireAuthorization();

        group.MapGet("/config", (IOptions<AgentOptions> options) =>
        {
            var opts = options.Value;
            return Results.Ok(new OrchestratorConfigResponse(
                opts.DefaultAgent,
                opts.DelegationVisibility.ToString(),
                opts.AgentsDirectory));
        });

        group.MapPut("/config", async (
            OrchestratorConfigUpdateRequest? request,
            IOptions<AgentOptions> options,
            IUserProfileService profileService,
            CancellationToken ct) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var opts = options.Value;

            if (request.DelegationVisibility is not null)
            {
                await profileService.SetExplicitPreferenceAsync(
                        VisibilityKey, request.DelegationVisibility, ConfigCategory, ct)
                    .ConfigureAwait(false);
            }

            return Results.Ok(new OrchestratorConfigResponse(
                opts.DefaultAgent,
                request.DelegationVisibility ?? opts.DelegationVisibility.ToString(),
                opts.AgentsDirectory));
        });

        return app;
    }
}

/// <summary>Orchestrator configuration response.</summary>
/// <param name="DefaultAgent">The default agent name.</param>
/// <param name="DelegationVisibility">How delegation is presented to the user.</param>
/// <param name="AgentsDirectory">Directory containing agent definitions.</param>
internal sealed record OrchestratorConfigResponse(
    string DefaultAgent,
    string DelegationVisibility,
    string AgentsDirectory);

/// <summary>Request body for updating orchestrator configuration.</summary>
internal sealed record OrchestratorConfigUpdateRequest(
    string? DelegationVisibility);
