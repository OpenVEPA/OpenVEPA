using FluentAssertions;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Tests.Skills;

public sealed class SkillResultTests
{
    [Fact]
    public void SuccessResult_HasNoError()
    {
        var data = new Dictionary<string, object?> { ["answer"] = "42" };
        var result = new SkillResult(Success: true, Data: data, Error: null, TokenUsage: null);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().ContainKey("answer");
    }

    [Fact]
    public void ErrorResult_HasErrorDetails()
    {
        var error = new SkillError(SkillErrorKind.Permanent, "Something failed");
        var result = new SkillResult(Success: false, Data: null, Error: error, TokenUsage: null);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Kind.Should().Be(SkillErrorKind.Permanent);
        result.Error.Message.Should().Be("Something failed");
    }

    [Fact]
    public void ErrorResult_WithInnerException_RetainsException()
    {
        var inner = new InvalidOperationException("root cause");
        var error = new SkillError(SkillErrorKind.Transient, "Wrapper", inner);
        var result = new SkillResult(Success: false, Data: null, Error: error, TokenUsage: null);

        result.Error!.Inner.Should().BeSameAs(inner);
    }

    [Fact]
    public void TokenUsage_TracksInputAndOutput()
    {
        var usage = new TokenUsage(InputTokens: 100, OutputTokens: 50, EstimatedCostUsd: 0.005m);
        var result = new SkillResult(Success: true, Data: null, Error: null, TokenUsage: usage);

        result.TokenUsage.Should().NotBeNull();
        result.TokenUsage!.InputTokens.Should().Be(100);
        result.TokenUsage.OutputTokens.Should().Be(50);
        result.TokenUsage.EstimatedCostUsd.Should().Be(0.005m);
    }

    [Fact]
    public void TokenUsage_TotalTokens_CanBeCalculated()
    {
        var usage = new TokenUsage(InputTokens: 200, OutputTokens: 75, EstimatedCostUsd: null);

        var total = usage.InputTokens + usage.OutputTokens;

        total.Should().Be(275);
    }

    [Fact]
    public void TokenUsage_WithNullCost_AllowsNull()
    {
        var usage = new TokenUsage(InputTokens: 10, OutputTokens: 5, EstimatedCostUsd: null);

        usage.EstimatedCostUsd.Should().BeNull();
    }

    [Theory]
    [InlineData(SkillErrorKind.Transient)]
    [InlineData(SkillErrorKind.Permanent)]
    [InlineData(SkillErrorKind.RateLimited)]
    [InlineData(SkillErrorKind.AuthFailed)]
    [InlineData(SkillErrorKind.Timeout)]
    public void SkillError_AllKinds_CanBeCreated(SkillErrorKind kind)
    {
        var error = new SkillError(kind, $"Error: {kind}");

        error.Kind.Should().Be(kind);
        error.Message.Should().Contain(kind.ToString());
    }

    [Fact]
    public void SuccessResult_WithTokenUsage_RetainsBoth()
    {
        var data = new Dictionary<string, object?> { ["key"] = "value" };
        var usage = new TokenUsage(50, 25, 0.001m);
        var result = new SkillResult(Success: true, Data: data, Error: null, TokenUsage: usage);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.TokenUsage!.InputTokens.Should().Be(50);
    }
}
