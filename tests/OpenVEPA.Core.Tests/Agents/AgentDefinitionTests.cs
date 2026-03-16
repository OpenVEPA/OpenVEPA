using FluentAssertions;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Tests.Agents;

public sealed class AgentDefinitionTests
{
    [Fact]
    public void AgentDefinition_Creation_RetainsAllProperties()
    {
        var skills = new List<string> { "search", "summarize" };
        var llm = new AgentLlmRequirements(Capabilities: ["streaming", "function-calling"]);

        var agent = new AgentDefinition(
            Name: "assistant",
            Description: "General-purpose assistant",
            SystemPrompt: "You are a helpful assistant.",
            Skills: skills,
            AutonomyLevel: 3,
            LlmRequirements: llm);

        agent.Name.Should().Be("assistant");
        agent.Description.Should().Be("General-purpose assistant");
        agent.SystemPrompt.Should().Be("You are a helpful assistant.");
        agent.Skills.Should().HaveCount(2);
        agent.Skills.Should().Contain("search");
        agent.AutonomyLevel.Should().Be(3);
        agent.LlmRequirements.Should().NotBeNull();
    }

    [Fact]
    public void AgentDefinition_WithNullLlmRequirements_AllowsNull()
    {
        var agent = new AgentDefinition(
            "minimal", "Minimal agent", "prompt", [], 1, LlmRequirements: null);

        agent.LlmRequirements.Should().BeNull();
    }

    [Fact]
    public void AgentLlmRequirements_Capabilities_AreAccessible()
    {
        var requirements = new AgentLlmRequirements(["streaming", "vision"]);

        requirements.Capabilities.Should().HaveCount(2);
        requirements.Capabilities.Should().Contain("vision");
    }

    [Fact]
    public void AgentLlmRequirements_EmptyCapabilities_ReturnsEmptyList()
    {
        var requirements = new AgentLlmRequirements([]);

        requirements.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public void AgentResponse_WithSkillResults_RetainsAll()
    {
        var skillResults = new List<SkillResult>
        {
            new(Success: true, Data: null, Error: null, TokenUsage: new TokenUsage(10, 5, null)),
            new(Success: false, Data: null, Error: new SkillError(SkillErrorKind.Timeout, "Timed out"), TokenUsage: null),
        };
        var totalUsage = new TokenUsage(100, 50, 0.01m);

        var response = new AgentResponse(
            Content: "Here are your results.",
            SkillResults: skillResults,
            TotalTokenUsage: totalUsage);

        response.Content.Should().Be("Here are your results.");
        response.SkillResults.Should().HaveCount(2);
        response.SkillResults![0].Success.Should().BeTrue();
        response.SkillResults[1].Success.Should().BeFalse();
        response.TotalTokenUsage!.InputTokens.Should().Be(100);
    }

    [Fact]
    public void AgentResponse_WithoutSkillResults_HasNull()
    {
        var response = new AgentResponse(
            Content: "Simple response",
            SkillResults: null,
            TotalTokenUsage: null);

        response.SkillResults.Should().BeNull();
        response.TotalTokenUsage.Should().BeNull();
    }

    [Fact]
    public void AgentResponse_WithOnlyTotalUsage_RetainsUsage()
    {
        var usage = new TokenUsage(200, 100, 0.03m);
        var response = new AgentResponse("Content", null, usage);

        response.TotalTokenUsage.Should().NotBeNull();
        response.TotalTokenUsage!.OutputTokens.Should().Be(100);
        response.TotalTokenUsage.EstimatedCostUsd.Should().Be(0.03m);
    }
}
