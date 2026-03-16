using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for discovered skills.</summary>
internal static class SkillsApiExtensions
{
    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/skills</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapSkillsApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder skills = app.MapGroup("/api/skills")
            .RequireAuthorization();

        skills.MapGet(string.Empty, (ISkillRuntime skillRuntime) =>
        {
            var skillSummaries = skillRuntime.ListSkills()
                .OrderBy(static manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static manifest => new SkillSummaryResponse(
                    manifest.Name,
                    manifest.Description,
                    manifest.Version,
                    manifest.Type))
                .ToArray();

            return Results.Ok(skillSummaries);
        });

        skills.MapGet("/{name}", (string name, ISkillRuntime skillRuntime) =>
        {
            var manifest = FindManifest(skillRuntime, name);
            return manifest is null ? Results.NotFound() : Results.Ok(manifest);
        });

        return app;
    }

    private static SkillManifest? FindManifest(ISkillRuntime skillRuntime, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return skillRuntime.ListSkills()
            .FirstOrDefault(manifest => string.Equals(manifest.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Represents the skill metadata returned by the skills list endpoint.</summary>
/// <param name="Name">The skill name.</param>
/// <param name="Description">The skill description.</param>
/// <param name="Version">The skill version.</param>
/// <param name="Type">The skill type.</param>
internal sealed record SkillSummaryResponse(
    string Name,
    string Description,
    string Version,
    SkillType Type);
