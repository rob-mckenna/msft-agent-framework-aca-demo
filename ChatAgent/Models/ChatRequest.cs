namespace ChatAgent.Models;

/// <summary>
/// Represents an incoming chat request.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The user's message to send to the agent.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Optional thread ID for continuing an existing conversation.
    /// If omitted, a new conversation thread is created.
    /// </summary>
    public string? ThreadId { get; set; }
}
