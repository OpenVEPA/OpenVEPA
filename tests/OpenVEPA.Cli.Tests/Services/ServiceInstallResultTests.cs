using FluentAssertions;

using OpenVEPA.Cli.Services;

namespace OpenVEPA.Cli.Tests.Services;

public sealed class ServiceInstallResultTests
{
    [Fact]
    public void Success_Result_Has_Correct_Properties()
    {
        var result = new ServiceInstallResult(true, "Service installed.", "Windows");

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Service installed.");
        result.Platform.Should().Be("Windows");
    }

    [Fact]
    public void Failure_Result_Has_Correct_Properties()
    {
        var result = new ServiceInstallResult(false, "Access denied.", "Linux");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Access denied.");
        result.Platform.Should().Be("Linux");
    }

    [Fact]
    public void Record_Equality_Identical_Results_Are_Equal()
    {
        var a = new ServiceInstallResult(true, "Installed", "Windows");
        var b = new ServiceInstallResult(true, "Installed", "Windows");

        a.Should().Be(b);
    }

    [Fact]
    public void Record_Equality_Different_Messages_Are_Not_Equal()
    {
        var a = new ServiceInstallResult(true, "Installed", "Windows");
        var b = new ServiceInstallResult(true, "Already installed", "Windows");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Record_Equality_Different_Success_Values_Are_Not_Equal()
    {
        var a = new ServiceInstallResult(true, "Done", "Linux");
        var b = new ServiceInstallResult(false, "Done", "Linux");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Record_Equality_Different_Platforms_Are_Not_Equal()
    {
        var a = new ServiceInstallResult(true, "Done", "Windows");
        var b = new ServiceInstallResult(true, "Done", "Linux");

        a.Should().NotBe(b);
    }

    [Theory]
    [InlineData("Windows")]
    [InlineData("Linux")]
    [InlineData("macOS")]
    public void Platform_Can_Be_Set_To_All_Supported_Values(string platform)
    {
        var result = new ServiceInstallResult(true, "OK", platform);

        result.Platform.Should().Be(platform);
    }

    [Fact]
    public void ToString_Contains_All_Property_Values()
    {
        var result = new ServiceInstallResult(true, "Service started.", "macOS");

        var text = result.ToString();

        text.Should().Contain("True");
        text.Should().Contain("Service started.");
        text.Should().Contain("macOS");
    }
}
