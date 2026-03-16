namespace OpenVEPA.Core.Skills;

/// <summary>Classifies a skill by its execution mechanism.</summary>
public enum SkillType
{
    /// <summary>Runs natively in the host process.</summary>
    Native,

    /// <summary>Bridges to an external MCP server.</summary>
    McpBridge,

    /// <summary>Combines native logic with MCP calls.</summary>
    Hybrid
}
