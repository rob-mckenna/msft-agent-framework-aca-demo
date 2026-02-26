using Azure.AI.OpenAI;
using Azure.Identity;
using ChatAgent.Models;
using OpenAI.Chat;

namespace ChatAgent.Services;

/// <summary>
/// Chat service that uses Azure OpenAI directly, backing the REST /chat endpoint.
/// For the full Agent Framework experience (multi-channel, Bot Framework protocol),
/// the agent is also exposed via POST /api/messages.
/// </summary>
public class ChatAgentService : IChatAgentService
{
    private readonly ILogger<ChatAgentService> _logger;
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;

    // In-memory conversation history keyed by thread ID, capped to avoid unbounded growth.
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
    private readonly Queue<string> _threadOrder = new();
    private const int MaxConversations = 500;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ChatAgentService(IConfiguration configuration, ILogger<ChatAgentService> logger)
    {
        _logger = logger;

        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var deployment = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        _systemPrompt = configuration["AzureOpenAI:SystemPrompt"] ?? "You are a helpful assistant.";

        var credential = new DefaultAzureCredential();
        _chatClient = new AzureOpenAIClient(new Uri(endpoint), credential).GetChatClient(deployment);
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var threadId = request.ThreadId ?? Guid.NewGuid().ToString();

        await _lock.WaitAsync(cancellationToken);
        List<ChatMessage> history;
        try
        {
            if (!_conversations.TryGetValue(threadId, out history!))
            {
                // Evict oldest conversation when the cap is reached.
                if (_conversations.Count >= MaxConversations)
                {
                    var oldest = _threadOrder.Dequeue();
                    _conversations.Remove(oldest);
                }
                history = [new SystemChatMessage(_systemPrompt)];
                _conversations[threadId] = history;
                _threadOrder.Enqueue(threadId);
            }

            history.Add(new UserChatMessage(request.Message));
        }
        finally
        {
            _lock.Release();
        }

        _logger.LogInformation("Sending message to Azure OpenAI on thread '{ThreadId}'.", threadId);

        var result = await _chatClient.CompleteChatAsync(history, cancellationToken: cancellationToken);
        var reply = result.Value.Content[0].Text;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            history.Add(new AssistantChatMessage(reply));
        }
        finally
        {
            _lock.Release();
        }

        return new ChatResponse
        {
            Message = reply,
            ThreadId = threadId
        };
    }
}
