using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenVEPA.Storage.Tests;

public sealed class SqliteTokenStoreTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture = new();
    private SqliteTokenStore _store = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _store = new SqliteTokenStore(_fixture.WriteQueue, _fixture.DbFactory, NullLogger<SqliteTokenStore>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateToken_ReturnsNonEmptyTokenAndId()
    {
        var result = await _store.CreateTokenAsync("test-token");

        result.Should().NotBeNull();
        result.TokenId.Should().NotBeNullOrEmpty();
        result.PlaintextToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateToken_WithCorrectToken_ReturnsTokenId()
    {
        var created = await _store.CreateTokenAsync("valid-token");

        var tokenId = await _store.ValidateTokenAsync(created.PlaintextToken);

        tokenId.Should().Be(created.TokenId);
    }

    [Fact]
    public async Task ValidateToken_WithWrongToken_ReturnsNull()
    {
        await _store.CreateTokenAsync("other-token");

        // Generate a different base64 token.
        var wrongToken = Convert.ToBase64String(new byte[32]);

        var result = await _store.ValidateTokenAsync(wrongToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_WithInvalidBase64_ReturnsNull()
    {
        var result = await _store.ValidateTokenAsync("not-valid-base64!!!");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_WithEmptyString_ReturnsNull()
    {
        var result = await _store.ValidateTokenAsync("");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeToken_ThenValidate_ReturnsNull()
    {
        var created = await _store.CreateTokenAsync("revokable");

        // Verify it works before revocation.
        var beforeRevoke = await _store.ValidateTokenAsync(created.PlaintextToken);
        beforeRevoke.Should().NotBeNull();

        await _store.RevokeTokenAsync(created.TokenId);

        var afterRevoke = await _store.ValidateTokenAsync(created.PlaintextToken);
        afterRevoke.Should().BeNull();
    }

    [Fact]
    public async Task ListTokens_ShowsCreatedTokens()
    {
        await _store.CreateTokenAsync("token-a");
        await _store.CreateTokenAsync("token-b");

        var tokens = await _store.ListTokensAsync();

        tokens.Should().HaveCountGreaterThanOrEqualTo(2);
        tokens.Should().Contain(t => t.Name == "token-a");
        tokens.Should().Contain(t => t.Name == "token-b");
    }

    [Fact]
    public async Task ListTokens_ShowsRevokedStatus()
    {
        var created = await _store.CreateTokenAsync("to-revoke");
        await _store.RevokeTokenAsync(created.TokenId);

        var tokens = await _store.ListTokensAsync();

        var revokedToken = tokens.First(t => t.Id == created.TokenId);
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateToken_GeneratesUniqueTokens()
    {
        var first = await _store.CreateTokenAsync("unique-1");
        var second = await _store.CreateTokenAsync("unique-2");

        first.PlaintextToken.Should().NotBe(second.PlaintextToken);
        first.TokenId.Should().NotBe(second.TokenId);
    }
}
