using System.Reflection;
using System.Runtime.Loader;

using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Loads native .NET skills from assemblies specified in SKILL.md metadata.
/// Each skill gets its own collectible <see cref="AssemblyLoadContext"/>
/// to support future unloading.
/// </summary>
public sealed class NativeSkillLoader
{
    private readonly ILogger<NativeSkillLoader> _logger;

    public NativeSkillLoader(ILogger<NativeSkillLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads a native .NET skill from the assembly specified in the SKILL.md frontmatter.
    /// Returns <c>null</c> if the assembly cannot be found, loaded, or does not contain an <see cref="ISkill"/> type.
    /// </summary>
    /// <param name="skillDirectory">Full path to the skill subdirectory containing the assembly.</param>
    /// <param name="assemblyFileName">Assembly file name from the <c>openvepa-dotnet-assembly</c> frontmatter field.</param>
    /// <param name="manifest">The parsed manifest to associate with the loaded skill.</param>
    public ISkill? Load(string skillDirectory, string assemblyFileName, SkillManifest manifest)
    {
        var assemblyPath = Path.Combine(skillDirectory, assemblyFileName);
        if (!File.Exists(assemblyPath))
        {
            _logger.LogError("Skill assembly not found: {Path}", assemblyPath);
            return null;
        }

        try
        {
            var context = new CollectibleLoadContext(manifest.Name);
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            return FindAndInstantiate(assembly, manifest);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or ReflectionTypeLoadException)
        {
            _logger.LogError(ex, "Failed to load skill assembly: {Path}", assemblyPath);
            return null;
        }
    }

    private ISkill? FindAndInstantiate(Assembly assembly, SkillManifest manifest)
    {
        Type? skillType = null;
        foreach (var type in assembly.GetExportedTypes())
        {
            if (!typeof(ISkill).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            skillType = type;
            break;
        }

        if (skillType is null)
        {
            _logger.LogError("No ISkill implementation found in assembly {Assembly}", assembly.FullName);
            return null;
        }

        try
        {
            var instance = Activator.CreateInstance(skillType);
            if (instance is ISkill skill)
            {
                _logger.LogInformation("Loaded native skill: {Name} from {Type}", manifest.Name, skillType.FullName);
                return skill;
            }

            _logger.LogError("Type {Type} implements ISkill but could not be cast", skillType.FullName);
            return null;
        }
        catch (Exception ex) when (ex is MissingMethodException or TargetInvocationException)
        {
            _logger.LogError(ex, "Failed to instantiate skill type {Type}", skillType.FullName);
            return null;
        }
    }

    /// <summary>
    /// A collectible assembly load context that isolates each skill's dependencies.
    /// </summary>
    private sealed class CollectibleLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Return null to fall back to the default context for shared assemblies
            return null;
        }
    }
}
