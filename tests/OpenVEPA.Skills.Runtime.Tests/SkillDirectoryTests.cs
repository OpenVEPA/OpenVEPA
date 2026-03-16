using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenVEPA.Skills.Runtime;

namespace OpenVEPA.Skills.Runtime.Tests;

public sealed class SkillDirectoryTests : IDisposable
{
    private readonly string _tempDir;

    public SkillDirectoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skilldir-test-{Guid.NewGuid():N}");
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
    public void Discover_EmptyDirectory_ReturnsEmptyList()
    {
        var directory = CreateSkillDirectory(_tempDir);

        var results = directory.Discover();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Discover_DirectoryWithSkillMd_FindsSkill()
    {
        var skillDir = Path.Combine(_tempDir, "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: my-skill
            ---
            # My Skill
            """);

        var directory = CreateSkillDirectory(_tempDir);

        var results = directory.Discover();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("my-skill");
        results[0].SkillMdPath.Should().EndWith("SKILL.md");
    }

    [Fact]
    public void Discover_DirectoryWithoutSkillMd_IsIgnored()
    {
        var noSkillDir = Path.Combine(_tempDir, "no-skill");
        Directory.CreateDirectory(noSkillDir);
        File.WriteAllText(Path.Combine(noSkillDir, "README.md"), "# Not a skill");

        var directory = CreateSkillDirectory(_tempDir);

        var results = directory.Discover();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Discover_MultipleSkillDirs_FindsAll()
    {
        CreateSkillSubDir("skill-a");
        CreateSkillSubDir("skill-b");
        CreateSkillSubDir("skill-c");

        var directory = CreateSkillDirectory(_tempDir);

        var results = directory.Discover();

        results.Should().HaveCount(3);
        results.Select(r => r.Name).Should().Contain(["skill-a", "skill-b", "skill-c"]);
    }

    [Fact]
    public void Discover_NonExistentDirectory_ReturnsEmptyList()
    {
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist");
        var directory = CreateSkillDirectory(nonExistentPath);

        var results = directory.Discover();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Discover_MixedDirectories_OnlyFindsSkills()
    {
        CreateSkillSubDir("valid-skill");

        var noSkillDir = Path.Combine(_tempDir, "just-files");
        Directory.CreateDirectory(noSkillDir);
        File.WriteAllText(Path.Combine(noSkillDir, "data.txt"), "some data");

        var directory = CreateSkillDirectory(_tempDir);

        var results = directory.Discover();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("valid-skill");
    }

    private void CreateSkillSubDir(string name)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"""
            ---
            name: {name}
            ---
            # {name}
            """);
    }

    private static SkillDirectory CreateSkillDirectory(string path)
    {
        var options = Options.Create(new SkillsOptions { SkillsDirectory = path });
        return new SkillDirectory(options, NullLogger<SkillDirectory>.Instance);
    }
}
