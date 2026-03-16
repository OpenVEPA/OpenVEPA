using FluentAssertions;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Tests.Skills;

public sealed class SkillManifestTests
{
    [Fact]
    public void Create_WithAllProperties_RetainsValues()
    {
        var inputs = new List<SkillParameter>
        {
            new("query", "string", "Search query", Required: true, DefaultValue: null),
            new("limit", "int", "Max results", Required: false, DefaultValue: 10),
        };

        var outputs = new List<SkillOutput>
        {
            new("results", "string[]", "Search results"),
        };

        var permissions = new SkillPermissions(
            AllowedTools: ["web-search"],
            RequiredCapabilities: ["internet"]);

        var manifest = new SkillManifest(
            Name: "search",
            Description: "Web search skill",
            Version: "1.2.0",
            Type: SkillType.Native,
            Inputs: inputs,
            Outputs: outputs,
            Permissions: permissions);

        manifest.Name.Should().Be("search");
        manifest.Description.Should().Be("Web search skill");
        manifest.Version.Should().Be("1.2.0");
        manifest.Type.Should().Be(SkillType.Native);
        manifest.Inputs.Should().HaveCount(2);
        manifest.Outputs.Should().HaveCount(1);
        manifest.Permissions.Should().NotBeNull();
        manifest.Permissions!.AllowedTools.Should().Contain("web-search");
    }

    [Fact]
    public void Create_WithNullPermissions_AllowsNull()
    {
        var manifest = new SkillManifest(
            Name: "simple",
            Description: "A simple skill",
            Version: "0.1.0",
            Type: SkillType.McpBridge,
            Inputs: [],
            Outputs: [],
            Permissions: null);

        manifest.Permissions.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_IdenticalManifests_AreEqual()
    {
        var inputs = new List<SkillParameter>
        {
            new("input", "string", "The input", Required: true, DefaultValue: null),
        };
        var outputs = new List<SkillOutput>
        {
            new("output", "string", "The output"),
        };

        var a = new SkillManifest("skill", "desc", "1.0.0", SkillType.Hybrid, inputs, outputs, null);
        var b = new SkillManifest("skill", "desc", "1.0.0", SkillType.Hybrid, inputs, outputs, null);

        a.Should().Be(b);
    }

    [Fact]
    public void RecordEquality_DifferentNames_AreNotEqual()
    {
        var a = new SkillManifest("alpha", "desc", "1.0.0", SkillType.Native, [], [], null);
        var b = new SkillManifest("beta", "desc", "1.0.0", SkillType.Native, [], [], null);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Inputs_AreAccessible_ByIndex()
    {
        var inputs = new List<SkillParameter>
        {
            new("first", "string", "First param", Required: true, DefaultValue: null),
            new("second", "int", "Second param", Required: false, DefaultValue: 42),
        };

        var manifest = new SkillManifest("test", "desc", "1.0.0", SkillType.Native, inputs, [], null);

        manifest.Inputs[0].Name.Should().Be("first");
        manifest.Inputs[0].Required.Should().BeTrue();
        manifest.Inputs[1].Name.Should().Be("second");
        manifest.Inputs[1].DefaultValue.Should().Be(42);
    }

    [Theory]
    [InlineData(SkillType.Native)]
    [InlineData(SkillType.McpBridge)]
    [InlineData(SkillType.Hybrid)]
    public void Create_WithEachSkillType_RetainsType(SkillType type)
    {
        var manifest = new SkillManifest("test", "desc", "1.0.0", type, [], [], null);

        manifest.Type.Should().Be(type);
    }
}
