namespace OpenVEPA.Storage.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void StorageAssembly_ShouldBeLoadable()
    {
        var marker = typeof(Storage.AssemblyMarker);
        Assert.NotNull(marker);
    }
}
