namespace OpenVEPA.Core.Agents;

/// <summary>Permissions controlling what an agent can access.</summary>
public sealed record AgentPermissions(
    /// <summary>Whether the agent may make internet requests.</summary>
    bool Internet = false,
    /// <summary>Whether the agent may access the file system.</summary>
    bool FileSystem = false,
    /// <summary>Whether the agent may execute arbitrary code.</summary>
    bool CodeExecution = false,
    /// <summary>Whether the agent may access databases.</summary>
    bool DatabaseAccess = false);
