using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OpenVEPA.Core.Skills;
using OpenVEPA.Skills.Runtime;

namespace OpenVEPA.Skills.Runtime.Tests;

public sealed class SkillMdParserTests : IDisposable
{
    private readonly SkillMdParser _parser;
    private readonly string _tempDir;

    public SkillMdParserTests()
    {
        _parser = new SkillMdParser(NullLogger<SkillMdParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillmd-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Parse_ValidSkillMd_ReturnsCorrectManifest()
    {
        var content = """
            ---
            name: test-skill
            description: A test skill
            version: "1.0.0"
            openvepa-type: native
            inputs:
              - name: query
                type: string
                description: The search query
                required: true
              - name: limit
                type: int
                description: Max results
                required: false
                default: 10
            outputs:
              - name: result
                type: string
                description: The output
            ---
            # Test Skill
            This is a test skill.
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Name.Should().Be("test-skill");
        manifest.Description.Should().Be("A test skill");
        manifest.Version.Should().Be("1.0.0");
        manifest.Type.Should().Be(SkillType.Native);
        manifest.Inputs.Should().HaveCount(2);
        manifest.Outputs.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_NoFrontmatter_ReturnsNull()
    {
        var content = """
            # Just Markdown
            No YAML frontmatter here.
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().BeNull();
    }

    [Fact]
    public void Parse_MissingName_ReturnsNull()
    {
        var content = """
            ---
            description: No name field
            version: "1.0.0"
            ---
            # Nameless
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().BeNull();
    }

    [Fact]
    public void Parse_NonExistentFile_ReturnsNull()
    {
        var filePath = Path.Combine(_tempDir, "nonexistent.md");
        var manifest = _parser.Parse(filePath);

        manifest.Should().BeNull();
    }

    [Fact]
    public void Parse_ParametersParsedCorrectly()
    {
        var content = """
            ---
            name: parameterized
            inputs:
              - name: input-text
                type: string
                description: The input text
                required: true
              - name: temperature
                type: float
                description: Sampling temperature
                required: false
                default: 0.7
            ---
            # Parameterized
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Inputs.Should().HaveCount(2);

        manifest.Inputs[0].Name.Should().Be("input-text");
        manifest.Inputs[0].Type.Should().Be("string");
        manifest.Inputs[0].Description.Should().Be("The input text");
        manifest.Inputs[0].Required.Should().BeTrue();

        manifest.Inputs[1].Name.Should().Be("temperature");
        manifest.Inputs[1].Type.Should().Be("float");
        manifest.Inputs[1].Required.Should().BeFalse();
        manifest.Inputs[1].DefaultValue.Should().NotBeNull();
    }

    [Fact]
    public void Parse_McpBridgeType_ReturnsCorrectType()
    {
        var content = """
            ---
            name: mcp-skill
            openvepa-type: mcp-bridge
            ---
            # MCP Skill
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Type.Should().Be(SkillType.McpBridge);
    }

    [Fact]
    public void Parse_NoType_DefaultsToMcpBridge()
    {
        var content = """
            ---
            name: default-type
            ---
            # Default Type Skill
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Type.Should().Be(SkillType.McpBridge);
    }

    [Fact]
    public void Parse_WithDotnetAssembly_ImpliesNative()
    {
        var content = """
            ---
            name: native-implied
            openvepa-dotnet-assembly: MyAssembly.dll
            ---
            # Native Implied
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Type.Should().Be(SkillType.Native);
    }

    [Fact]
    public void Parse_VersionFallback_UsesDefault()
    {
        var content = """
            ---
            name: no-version
            ---
            # No Version
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Version.Should().Be("0.0.0");
    }

    [Fact]
    public void Parse_OutputsParsedCorrectly()
    {
        var content = """
            ---
            name: with-outputs
            outputs:
              - name: summary
                type: string
                description: The summary text
              - name: confidence
                type: float
                description: Confidence score
            ---
            # With Outputs
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Outputs.Should().HaveCount(2);
        manifest.Outputs[0].Name.Should().Be("summary");
        manifest.Outputs[0].Type.Should().Be("string");
        manifest.Outputs[1].Name.Should().Be("confidence");
        manifest.Outputs[1].Type.Should().Be("float");
    }

    [Fact]
    public void Parse_WithPermissions_ParsesCorrectly()
    {
        var content = """
            ---
            name: with-perms
            permissions:
              allowed-tools:
                - web-search
                - file-read
              required-capabilities:
                - internet
            ---
            # With Permissions
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Permissions.Should().NotBeNull();
        manifest.Permissions!.AllowedTools.Should().HaveCount(2);
        manifest.Permissions.AllowedTools.Should().Contain("web-search");
        manifest.Permissions.RequiredCapabilities.Should().Contain("internet");
    }

    [Fact]
    public void Parse_EmptyFrontmatter_WithName_ReturnsMinimalManifest()
    {
        var content = """
            ---
            name: minimal
            ---
            # Minimal
            """;

        var filePath = WriteSkillMd(content);
        var manifest = _parser.Parse(filePath);

        manifest.Should().NotBeNull();
        manifest!.Name.Should().Be("minimal");
        manifest.Inputs.Should().BeEmpty();
        manifest.Outputs.Should().BeEmpty();
        manifest.Permissions.Should().BeNull();
    }

    private string WriteSkillMd(string content)
    {
        var filePath = Path.Combine(_tempDir, $"SKILL-{Guid.NewGuid():N}.md");
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
