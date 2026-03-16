using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Agents;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for discovered agents.</summary>
internal static class AgentsApiExtensions
{
    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/agents</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapAgentsApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder agents = app.MapGroup("/api/agents")
            .RequireAuthorization();

        agents.MapGet(string.Empty, (AgentDirectory agentDirectory) =>
        {
            var agents = agentDirectory.DiscoverAgents()
                .OrderBy(static agent => agent.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static agent => new AgentSummaryResponse(
                    agent.Name,
                    agent.Description,
                    agent.AutonomyLevel,
                    agent.Skills.Count))
                .ToArray();

            return Results.Ok(agents);
        });

        agents.MapGet("/{name}", (string name, AgentDirectory agentDirectory) =>
        {
            var agent = FindAgent(agentDirectory, name);
            return agent is null ? Results.NotFound() : Results.Ok(agent);
        });

        return app;
    }

    private static AgentDefinition? FindAgent(AgentDirectory agentDirectory, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return agentDirectory.DiscoverAgents()
            .FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Represents the agent metadata returned by the agents list endpoint.</summary>
/// <param name="Name">The agent name.</param>
/// <param name="Description">The agent description.</param>
/// <param name="AutonomyLevel">The default autonomy level for the agent.</param>
/// <param name="SkillCount">The number of skills referenced by the agent.</param>
internal sealed record AgentSummaryResponse(
    string Name,
    string Description,
    int AutonomyLevel,
    int SkillCount);
