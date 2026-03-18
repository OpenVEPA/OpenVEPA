using Microsoft.Extensions.AI;

namespace OpenVEPA.Core.Agents;

/// <summary>
/// Creates <see cref="IChatClient"/> instances tailored to a specific agent's LLM configuration.
/// Uses the agent's <see cref="AgentLlmConfig"/> when set, otherwise falls back to the system default provider.
/// </summary>
public interface IAgentChatClientFactory
{
    /// <summary>
    /// Gets an <see cref="IChatClient"/> for the given agent.
    /// The returned client applies agent-specific overrides (model, temperature, max tokens)
    /// when the agent defines an <see cref="AgentLlmConfig"/>.
    /// </summary>
    /// <param name="agent">The agent definition whose LLM configuration drives provider selection.</param>
    /// <returns>A configured <see cref="IChatClient"/> ready for use.</returns>
    IChatClient GetChatClient(AgentDefinition agent);
}
