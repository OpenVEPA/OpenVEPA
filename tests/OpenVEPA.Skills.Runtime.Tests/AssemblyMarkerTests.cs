namespace OpenVEPA.Skills.Runtime.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void SkillsRuntimeAssembly_ShouldBeLoadable()
    {
        var marker = typeof(Skills.Runtime.AssemblyMarker);
        Assert.NotNull(marker);
    }
}
