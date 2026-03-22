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
    bool DatabaseAccess = false)
{
    /// <summary>
    /// Checks whether the given capability string is allowed by this permission set.
    /// Recognised capability names (case-insensitive): internet, filesystem / file-system,
    /// codeexecution / code-execution, databaseaccess / database-access / database.
    /// Unrecognised capabilities are denied by default.
    /// </summary>
    public bool IsAllowed(string capability)
    {
        return capability.ToLowerInvariant() switch
        {
            "internet" => Internet,
            "filesystem" or "file-system" => FileSystem,
            "codeexecution" or "code-execution" => CodeExecution,
            "databaseaccess" or "database-access" or "database" => DatabaseAccess,
            _ => false
        };
    }
}
