using OpenVEPA.Core.Skills;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Stub implementation for MCP bridge skills.
/// Phase 1: returns a result indicating the MCP bridge is not yet implemented.
/// Future phases will start an MCP server process per invocation and bridge calls.
/// </summary>
public sealed class McpBridgeSkill : ISkill
{
    public McpBridgeSkill(SkillManifest manifest)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    /// <inheritdoc />
    public SkillManifest Manifest { get; }

    /// <inheritdoc />
    public Task<SkillResult> ExecuteAsync(SkillInput input, SkillExecutionContext context, CancellationToken ct)
    {
        var error = new SkillError(
            SkillErrorKind.Permanent,
            $"MCP bridge skill '{Manifest.Name}' is not yet implemented. MCP integration is planned for a future phase.");

        var result = new SkillResult(
            Success: false,
            Data: null,
            Error: error,
            TokenUsage: null);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // No resources to release in the stub implementation.
        return ValueTask.CompletedTask;
    }
}
