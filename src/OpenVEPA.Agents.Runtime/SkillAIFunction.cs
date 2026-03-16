using System.Text.Json;

using Microsoft.Extensions.AI;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Agents.Runtime;

/// <summary>Wraps a <see cref="SkillManifest"/> as an <see cref="AIFunction"/> for LLM tool calling.</summary>
internal sealed class SkillAIFunction : AIFunction
{
    private readonly string _skillName;
    private readonly ISkillRuntime _skillRuntime;
    private readonly SkillExecutionContext _executionContext;

    private readonly JsonElement _jsonSchema;

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override string Description { get; }

    /// <inheritdoc />
    public override JsonElement JsonSchema => _jsonSchema;

    /// <summary>Initializes a new instance wrapping the specified skill manifest.</summary>
    public SkillAIFunction(
        SkillManifest manifest,
        ISkillRuntime skillRuntime,
        SkillExecutionContext executionContext)
    {
        if (manifest is null) throw new ArgumentNullException(nameof(manifest));

        _skillRuntime = skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _skillName = manifest.Name;

        Name = manifest.Name;
        Description = manifest.Description;
        _jsonSchema = BuildJsonSchema(manifest.Inputs);
    }

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        foreach (var kvp in arguments)
            parameters[kvp.Key] = kvp.Value;

        var prompt = parameters.TryGetValue("prompt", out var promptVal)
            ? promptVal?.ToString() ?? ""
            : "";

        var input = new SkillInput(prompt, parameters);
        var result = await _skillRuntime.ExecuteAsync(
            _skillName, input, _executionContext, cancellationToken);

        if (!result.Success)
            return JsonSerializer.Serialize(new { error = result.Error?.Message ?? "Unknown error" });

        return result.Data is not null
            ? JsonSerializer.Serialize(result.Data)
            : JsonSerializer.Serialize(new { status = "success" });
    }

    private static JsonElement BuildJsonSchema(IReadOnlyList<SkillParameter> inputs)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in inputs)
        {
            properties[param.Name] = new Dictionary<string, object>
            {
                ["type"] = MapParameterType(param.Type),
                ["description"] = param.Description
            };

            if (param.Required)
                required.Add(param.Name);
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
            schema["required"] = required;

        var json = JsonSerializer.Serialize(schema);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static string MapParameterType(string skillType)
    {
        return skillType.ToLowerInvariant() switch
        {
            "string" or "text" => "string",
            "int" or "integer" => "integer",
            "number" or "float" or "double" or "decimal" => "number",
            "bool" or "boolean" => "boolean",
            "array" or "list" => "array",
            _ => "string"
        };
    }
}
