namespace OpenVEPA.Server.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void ServerAssembly_ShouldBeLoadable()
    {
        var marker = typeof(Server.AssemblyMarker);
        Assert.NotNull(marker);
    }
}
