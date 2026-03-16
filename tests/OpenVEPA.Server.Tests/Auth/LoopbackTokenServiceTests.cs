using System.Net;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OpenVEPA.Server.Auth;

namespace OpenVEPA.Server.Tests.Auth;

public sealed class LoopbackTokenServiceTests : IAsyncLifetime
{
    private readonly LoopbackTokenService _service;

    public LoopbackTokenServiceTests()
    {
        _service = new LoopbackTokenService(NullLogger<LoopbackTokenService>.Instance);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Ensure cleanup even if test fails mid-way.
        await _service.StopAsync(CancellationToken.None);
        _service.Dispose();
    }

    [Fact]
    public void Token_BeforeStartAsync_IsNull()
    {
        _service.Token.Should().BeNull();
    }

    [Fact]
    public async Task Token_AfterStartAsync_IsNotNull()
    {
        await _service.StartAsync(CancellationToken.None);

        _service.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_CorrectTokenAndLoopbackV4_ReturnsTrue()
    {
        await _service.StartAsync(CancellationToken.None);

        var result = _service.Validate(_service.Token!, IPAddress.Loopback);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_CorrectTokenAndLoopbackV6_ReturnsTrue()
    {
        await _service.StartAsync(CancellationToken.None);

        var result = _service.Validate(_service.Token!, IPAddress.IPv6Loopback);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WrongToken_ReturnsFalse()
    {
        await _service.StartAsync(CancellationToken.None);

        var result = _service.Validate("wrong-token", IPAddress.Loopback);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NonLoopbackAddress_ReturnsFalse()
    {
        await _service.StartAsync(CancellationToken.None);

        var externalIp = IPAddress.Parse("192.168.1.100");
        var result = _service.Validate(_service.Token!, externalIp);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NullAddress_ReturnsFalse()
    {
        await _service.StartAsync(CancellationToken.None);

        var result = _service.Validate(_service.Token!, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyToken_ReturnsFalse()
    {
        await _service.StartAsync(CancellationToken.None);

        var result = _service.Validate("", IPAddress.Loopback);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_CleansUpTokenFile()
    {
        await _service.StartAsync(CancellationToken.None);

        var tokenFilePath = _service.TokenFilePath;
        tokenFilePath.Should().NotBeNull();
        File.Exists(tokenFilePath).Should().BeTrue();

        await _service.StopAsync(CancellationToken.None);

        File.Exists(tokenFilePath).Should().BeFalse();
        _service.Token.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_WritesTokenFile()
    {
        await _service.StartAsync(CancellationToken.None);

        _service.TokenFilePath.Should().NotBeNull();
        File.Exists(_service.TokenFilePath).Should().BeTrue();

        var fileContent = await File.ReadAllTextAsync(_service.TokenFilePath!);
        fileContent.Should().Be(_service.Token);
    }

    [Fact]
    public async Task StartAsync_GeneratesDifferentTokensEachTime()
    {
        await _service.StartAsync(CancellationToken.None);
        var firstToken = _service.Token;
        await _service.StopAsync(CancellationToken.None);

        await _service.StartAsync(CancellationToken.None);
        var secondToken = _service.Token;

        firstToken.Should().NotBe(secondToken);
    }
}
