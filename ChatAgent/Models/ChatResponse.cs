namespace ChatAgent.Models;

/// <summary>
/// Represents the agent's response to a chat request.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The agent's reply message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// The thread ID for this conversation.
    /// Use this value in subsequent requests to continue the same conversation.
    /// </summary>
    public required string ThreadId { get; set; }
}
