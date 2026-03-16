using System.Runtime.InteropServices;

using FluentAssertions;

using OpenVEPA.Cli.Services;

namespace OpenVEPA.Cli.Tests.Services;

public sealed class PlatformServiceInstallerTests
{
    [Fact]
    public void GetPlatformName_Returns_NonNull_NonEmpty_String()
    {
        var name = PlatformServiceInstaller.GetPlatformName();

        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetPlatformName_Returns_Correct_Value_For_Current_OS()
    {
        var name = PlatformServiceInstaller.GetPlatformName();

        if (OperatingSystem.IsWindows())
        {
            name.Should().Be("Windows");
        }
        else if (OperatingSystem.IsLinux())
        {
            name.Should().Be("Linux");
        }
        else if (OperatingSystem.IsMacOS())
        {
            name.Should().Be("macOS");
        }
        else
        {
            // Fallback: returns RuntimeInformation.OSDescription
            name.Should().Be(RuntimeInformation.OSDescription);
        }
    }

    [Fact]
    public void GetPlatformName_Returns_Known_Platform_On_Standard_OS()
    {
        var name = PlatformServiceInstaller.GetPlatformName();
        var knownPlatforms = new[] { "Windows", "Linux", "macOS" };

        // On standard CI/test machines, we expect a known platform.
        // On exotic OSes this test still passes because we only assert non-empty above.
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            knownPlatforms.Should().Contain(name);
        }
    }

    [Fact]
    public void IsInstalled_Returns_Bool_Without_Throwing()
    {
        var act = () => PlatformServiceInstaller.IsInstalled();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsInstalled_Returns_False_When_Service_Not_Registered()
    {
        // On a clean test environment the openvepa service should not be installed.
        var installed = PlatformServiceInstaller.IsInstalled();

        installed.Should().BeFalse();
    }

    [Fact]
    public async Task InstallAsync_With_Null_Path_Throws_ArgumentNullException()
    {
        var act = () => PlatformServiceInstaller.InstallAsync(null!, 8371, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InstallAsync_With_NonExistent_Executable_Returns_Failure()
    {
        var result = await PlatformServiceInstaller.InstallAsync(
            "/nonexistent/path/openvepa",
            8371,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
        result.Platform.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_Returns_ServiceInstallResult()
    {
        var result = await PlatformServiceInstaller.GetStatusAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.Platform.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UninstallAsync_Returns_ServiceInstallResult()
    {
        var result = await PlatformServiceInstaller.UninstallAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.Platform.Should().NotBeNullOrEmpty();
    }
}
