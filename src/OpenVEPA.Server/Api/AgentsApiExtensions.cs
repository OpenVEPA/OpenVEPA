using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for discovered agents.</summary>
internal static class AgentsApiExtensions
{
    private const string ConfigCategory = "agent-config";

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

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
                .OrderByDescending(static agent => agent.IsSystem)
                .ThenBy(static agent => agent.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static agent => new AgentSummaryResponse(
                    agent.Name,
                    agent.Description,
                    agent.AutonomyLevel,
                    agent.Skills.Count,
                    agent.IsSystem,
                    agent.Priority,
                    agent.Skills,
                    agent.LlmConfig is not null,
                    agent.TokenBudget is not null,
                    agent.Triggers?.Count ?? 0))
                .ToArray();

            return Results.Ok(agents);
        });

        agents.MapGet("/{name}", (string name, AgentDirectory agentDirectory) =>
        {
            var definition = FindAgent(agentDirectory, name);
            if (definition is null)
            {
                return Results.NotFound();
            }

            var response = new AgentDetailResponse(
                definition.Name,
                definition.Description,
                definition.AutonomyLevel,
                definition.Skills,
                definition.IsSystem,
                definition.Priority,
                definition.LlmConfig,
                definition.Permissions,
                definition.TokenBudget,
                definition.Triggers,
                definition.Restrictions);

            return Results.Ok(response);
        });

        agents.MapPut("/{name}/config", async (
            string name,
            AgentConfigUpdateRequest? request,
            AgentDirectory agentDirectory,
            IUserProfileService profileService,
            CancellationToken ct) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var agent = FindAgent(agentDirectory, name);
            if (agent is null)
            {
                return Results.NotFound();
            }

            var json = JsonSerializer.Serialize(request, s_jsonOptions);
            var preferenceKey = $"agent-config:{agent.Name}";

            await profileService.SetExplicitPreferenceAsync(
                    preferenceKey, json, ConfigCategory, ct)
                .ConfigureAwait(false);

            return Results.Ok(request);
        });

        agents.MapGet("/{name}/budget", async (
            string name,
            AgentDirectory agentDirectory,
            IAgentTokenBudgetTracker budgetTracker,
            CancellationToken ct) =>
        {
            var agent = FindAgent(agentDirectory, name);
            if (agent is null)
            {
                return Results.NotFound();
            }

            var summary = await budgetTracker.GetUsageSummaryAsync(
                    agent.Name, agent.TokenBudget, ct)
                .ConfigureAwait(false);

            return Results.Ok(summary);
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
/// <param name="IsSystem">Whether the agent is a built-in system agent.</param>
/// <param name="Priority">The agent priority for delegation ordering.</param>
/// <param name="Skills">The skill names referenced by the agent.</param>
/// <param name="HasLlmOverride">Whether the agent has a custom LLM configuration.</param>
/// <param name="HasBudget">Whether the agent has a token budget configured.</param>
/// <param name="TriggerCount">The number of delegation triggers defined.</param>
internal sealed record AgentSummaryResponse(
    string Name,
    string Description,
    int AutonomyLevel,
    int SkillCount,
    bool IsSystem,
    int Priority,
    IReadOnlyList<string> Skills,
    bool HasLlmOverride,
    bool HasBudget,
    int TriggerCount);

/// <summary>Curated agent detail response (excludes the system prompt).</summary>
internal sealed record AgentDetailResponse(
    string Name,
    string Description,
    int AutonomyLevel,
    IReadOnlyList<string> Skills,
    bool IsSystem,
    int Priority,
    AgentLlmConfig? LlmConfig,
    AgentPermissions? Permissions,
    AgentTokenBudget? TokenBudget,
    IReadOnlyList<string>? Triggers,
    IReadOnlyList<string>? Restrictions);

/// <summary>Request body for updating an agent's configuration.</summary>
internal sealed record AgentConfigUpdateRequest(
    AgentLlmConfig? LlmConfig,
    AgentPermissions? Permissions,
    AgentTokenBudget? TokenBudget,
    IReadOnlyList<string>? Triggers,
    IReadOnlyList<string>? Restrictions,
    int? Priority);
