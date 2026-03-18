using OpenVEPA.Core.Agents;

namespace OpenVEPA.Agents.Runtime;

/// <summary>Configuration options for the agent runtime.</summary>
public sealed class AgentOptions
{
    /// <summary>Directory path containing agent definition subdirectories.</summary>
    public string AgentsDirectory { get; set; } = "agents";

    /// <summary>Name of the default agent to use when none is specified.</summary>
    public string DefaultAgent { get; set; } = "assistant";

    /// <summary>Controls how delegation to specialist agents is presented to the user.</summary>
    public DelegationVisibility DelegationVisibility { get; set; } = DelegationVisibility.Invisible;
}
