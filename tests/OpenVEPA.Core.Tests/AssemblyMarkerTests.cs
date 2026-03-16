namespace OpenVEPA.Core.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void CoreAssembly_ShouldBeLoadable()
    {
        var marker = typeof(Core.AssemblyMarker);
        Assert.NotNull(marker);
    }
}
