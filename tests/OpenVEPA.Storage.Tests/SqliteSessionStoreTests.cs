using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;

namespace OpenVEPA.Storage.Tests;

public sealed class SqliteSessionStoreTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture = new();
    private SqliteSessionStore _store = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _store = new SqliteSessionStore(
            _fixture.WriteQueue,
            _fixture.DbFactory,
            NullLogger<SqliteSessionStore>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateSession_ReturnsSessionWithGeneratedId()
    {
        var session = await _store.CreateSessionAsync("Test Session");

        session.Should().NotBeNull();
        session.Id.Should().NotBeNullOrEmpty();
        session.Title.Should().Be("Test Session");
        session.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task GetSession_UnknownId_ReturnsNull()
    {
        var result = await _store.GetSessionAsync("nonexistent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListSessions_Initially_ReturnsEmptyList()
    {
        var sessions = await _store.ListSessionsAsync();

        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMessage_ThenGetMessages_ReturnsTheMessage()
    {
        var session = await _store.CreateSessionAsync("Chat");

        var message = new Message(
            Id: Guid.NewGuid().ToString("N"),
            SessionId: session.Id,
            Role: MessageRole.User,
            Content: "Hello, world!",
            Timestamp: DateTime.UtcNow,
            Usage: null);

        await _store.AddMessageAsync(message);

        var messages = await _store.GetMessagesAsync(session.Id);

        messages.Should().HaveCount(1);
        messages[0].Content.Should().Be("Hello, world!");
        messages[0].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public async Task GetMessages_OrderedByTimestamp()
    {
        var session = await _store.CreateSessionAsync("Ordered");
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var msg1 = new Message(Guid.NewGuid().ToString("N"), session.Id, MessageRole.User,
            "First", baseTime, null);
        var msg2 = new Message(Guid.NewGuid().ToString("N"), session.Id, MessageRole.Assistant,
            "Second", baseTime.AddMinutes(1), null);
        var msg3 = new Message(Guid.NewGuid().ToString("N"), session.Id, MessageRole.User,
            "Third", baseTime.AddMinutes(2), null);

        await _store.AddMessageAsync(msg3);
        await _store.AddMessageAsync(msg1);
        await _store.AddMessageAsync(msg2);

        var messages = await _store.GetMessagesAsync(session.Id);

        messages.Should().HaveCount(3);
        messages[0].Content.Should().Be("First");
        messages[1].Content.Should().Be("Second");
        messages[2].Content.Should().Be("Third");
    }

    [Fact]
    public async Task ListSessions_SortedByUpdatedAtDescending()
    {
        var first = await _store.CreateSessionAsync("First");

        // Small delay to ensure different UpdatedAt timestamps.
        await Task.Delay(50);

        var second = await _store.CreateSessionAsync("Second");

        var sessions = await _store.ListSessionsAsync();

        sessions.Should().HaveCount(2);
        sessions[0].Title.Should().Be("Second");
        sessions[1].Title.Should().Be("First");
    }

    [Fact]
    public async Task CreateSession_ThenGetSession_RoundTrips()
    {
        var created = await _store.CreateSessionAsync("Roundtrip");

        var fetched = await _store.GetSessionAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Title.Should().Be("Roundtrip");
        fetched.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task AddMessage_WithTokenUsage_PreservesUsage()
    {
        var session = await _store.CreateSessionAsync("Usage");

        var usage = new TokenUsage(100, 50, null);
        var message = new Message(
            Guid.NewGuid().ToString("N"), session.Id, MessageRole.Assistant,
            "Response", DateTime.UtcNow, usage);

        await _store.AddMessageAsync(message);

        var messages = await _store.GetMessagesAsync(session.Id);

        messages[0].Usage.Should().NotBeNull();
        messages[0].Usage!.InputTokens.Should().Be(100);
        messages[0].Usage!.OutputTokens.Should().Be(50);
    }
}
