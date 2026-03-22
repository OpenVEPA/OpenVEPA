using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Core.Skills;

/// <summary>Provides ambient context for a skill execution.</summary>
public sealed record SkillExecutionContext(
    IChatClient? ChatClient,
    ILogger Logger,
    IConfiguration Configuration,
    UserPreferences? UserPreferences,
    CancellationToken CancellationToken,
    AgentPermissions? Permissions = null);
