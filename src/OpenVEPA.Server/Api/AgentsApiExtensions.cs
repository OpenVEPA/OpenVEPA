using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for discovered agents.</summary>
internal static class AgentsApiExtensions
{
    private const string ConfigCategory = "agent-config";

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/agents</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapAgentsApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("OpenVEPA.Api.Agents");

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

            logger.LogDebug("Listing {Count} agents", agents.Length);
            return Results.Ok(agents);
        });

        agents.MapGet("/{name}", async (
            string name,
            AgentDirectory agentDirectory,
            IUserProfileService profileService,
            CancellationToken ct) =>
        {
            var definition = FindAgent(agentDirectory, name);
            if (definition is null)
            {
                return Results.NotFound();
            }

            // Load saved config from preferences and merge over base definition.
            var preferenceKey = $"agent-config:{definition.Name}";
            var savedConfig = await LoadSavedAgentConfigAsync(profileService, preferenceKey, logger, ct)
                .ConfigureAwait(false);

            logger.LogDebug("Loading agent config for {AgentName}, hasSavedConfig={HasSaved}",
                name, savedConfig is not null);

            var mergedDefinition = savedConfig is not null
                ? MergeConfig(definition, savedConfig)
                : definition;

            var response = new AgentDetailResponse(
                mergedDefinition.Name,
                mergedDefinition.Description,
                mergedDefinition.AutonomyLevel,
                mergedDefinition.Skills,
                mergedDefinition.IsSystem,
                mergedDefinition.Priority,
                mergedDefinition.LlmConfig,
                mergedDefinition.Permissions,
                mergedDefinition.TokenBudget,
                mergedDefinition.Triggers,
                mergedDefinition.Restrictions);

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

            logger.LogInformation("Saving agent config for {AgentName}: provider={Provider}, model={Model}",
                agent.Name,
                request.LlmConfig?.Provider ?? "(default)",
                request.LlmConfig?.Model ?? "(default)");

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

    private static async Task<AgentConfigUpdateRequest?> LoadSavedAgentConfigAsync(
        IUserProfileService profileService,
        string preferenceKey,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            var preferences = await profileService.GetRelevantPreferencesAsync(ConfigCategory, ct)
                .ConfigureAwait(false);

            if (preferences.Entries.TryGetValue(preferenceKey, out var entry)
                && !string.IsNullOrWhiteSpace(entry.Value))
            {
                logger.LogDebug("Found saved agent preference for key {PreferenceKey}", preferenceKey);
                return JsonSerializer.Deserialize<AgentConfigUpdateRequest>(entry.Value, s_jsonOptions);
            }

            logger.LogDebug("No saved agent preference found for key {PreferenceKey}", preferenceKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load saved agent preference for key {PreferenceKey}", preferenceKey);
        }

        return null;
    }

    private static AgentDefinition MergeConfig(AgentDefinition baseDefinition, AgentConfigUpdateRequest saved)
    {
        return baseDefinition with
        {
            LlmConfig = saved.LlmConfig ?? baseDefinition.LlmConfig,
            Permissions = saved.Permissions ?? baseDefinition.Permissions,
            TokenBudget = saved.TokenBudget ?? baseDefinition.TokenBudget,
            Triggers = saved.Triggers ?? baseDefinition.Triggers,
            Restrictions = saved.Restrictions ?? baseDefinition.Restrictions,
            Priority = saved.Priority ?? baseDefinition.Priority
        };
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
