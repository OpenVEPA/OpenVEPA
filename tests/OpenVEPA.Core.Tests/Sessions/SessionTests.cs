using FluentAssertions;

using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Tests.Sessions;

public sealed class SessionTests
{
    [Theory]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Archived)]
    public void Session_Creation_WithAllStatuses(SessionStatus status)
    {
        var now = DateTime.UtcNow;
        var session = new Session(
            Id: "session-1",
            Title: "Test Session",
            CreatedAt: now,
            UpdatedAt: now,
            Status: status);

        session.Id.Should().Be("session-1");
        session.Title.Should().Be("Test Session");
        session.CreatedAt.Should().Be(now);
        session.UpdatedAt.Should().Be(now);
        session.Status.Should().Be(status);
    }

    [Fact]
    public void Session_WithNullTitle_AllowsNull()
    {
        var session = new Session("id", null, DateTime.UtcNow, DateTime.UtcNow, SessionStatus.Active);

        session.Title.Should().BeNull();
    }

    [Theory]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    [InlineData(MessageRole.System)]
    [InlineData(MessageRole.Tool)]
    public void Message_Creation_ForEachRole(MessageRole role)
    {
        var timestamp = DateTime.UtcNow;
        var message = new Message(
            Id: "msg-1",
            SessionId: "session-1",
            Role: role,
            Content: "Hello",
            Timestamp: timestamp,
            Usage: null);

        message.Id.Should().Be("msg-1");
        message.SessionId.Should().Be("session-1");
        message.Role.Should().Be(role);
        message.Content.Should().Be("Hello");
        message.Timestamp.Should().Be(timestamp);
        message.Usage.Should().BeNull();
    }

    [Fact]
    public void Message_WithTokenUsage_RetainsUsage()
    {
        var usage = new TokenUsage(InputTokens: 50, OutputTokens: 30, EstimatedCostUsd: 0.002m);
        var message = new Message("msg-2", "session-1", MessageRole.Assistant, "Response", DateTime.UtcNow, usage);

        message.Usage.Should().NotBeNull();
        message.Usage!.InputTokens.Should().Be(50);
        message.Usage.OutputTokens.Should().Be(30);
    }

    [Fact]
    public void Message_WithoutTokenUsage_HasNullUsage()
    {
        var message = new Message("msg-3", "session-1", MessageRole.User, "Question", DateTime.UtcNow, null);

        message.Usage.Should().BeNull();
    }

    [Fact]
    public void Session_RecordEquality_SameValues_AreEqual()
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Session("id-1", "Title", now, now, SessionStatus.Active);
        var b = new Session("id-1", "Title", now, now, SessionStatus.Active);

        a.Should().Be(b);
    }

    [Fact]
    public void Session_RecordEquality_DifferentIds_AreNotEqual()
    {
        var now = DateTime.UtcNow;
        var a = new Session("id-1", "Title", now, now, SessionStatus.Active);
        var b = new Session("id-2", "Title", now, now, SessionStatus.Active);

        a.Should().NotBe(b);
    }
}
