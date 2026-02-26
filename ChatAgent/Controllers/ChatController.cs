using ChatAgent.Models;
using ChatAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatAgent.Controllers;

/// <summary>
/// Handles chat interactions with the Azure AI Foundry agent.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatAgentService _chatAgentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatAgentService chatAgentService, ILogger<ChatController> logger)
    {
        _chatAgentService = chatAgentService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to the AI agent and returns its response.
    /// </summary>
    /// <param name="request">The chat request with the user's message and optional thread ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response and the thread ID for conversation continuity.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponse>> PostAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty.");
        }

        _logger.LogInformation("Received chat message on thread '{ThreadId}'.", request.ThreadId ?? "new");

        var response = await _chatAgentService.ChatAsync(request, cancellationToken);
        return Ok(response);
    }
}
