using ChatAgent.Models;

namespace ChatAgent.Services;

/// <summary>
/// Provides chat interaction with the Azure AI Foundry agent.
/// </summary>
public interface IChatAgentService
{
    /// <summary>
    /// Sends a message to the agent and returns its response.
    /// </summary>
    /// <param name="request">The chat request containing the message and optional thread ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response, including the thread ID for conversation continuity.</returns>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
