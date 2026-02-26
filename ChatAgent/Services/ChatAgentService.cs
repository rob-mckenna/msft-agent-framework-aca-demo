#pragma warning disable SKEXP0110
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using ChatAgent.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatAgent.Services;

/// <summary>
/// Chat service backed by an Azure AI Foundry agent using the Microsoft Semantic Kernel Agent Framework.
/// </summary>
public class ChatAgentService : IChatAgentService, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatAgentService> _logger;
    private (AzureAIAgent Agent, PersistentAgentsClient Client)? _agentState;
    private string? _persistentAgentId;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public ChatAgentService(IConfiguration configuration, ILogger<ChatAgentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var (agent, client) = await GetOrCreateAgentAsync(cancellationToken);

        AzureAIAgentThread thread = string.IsNullOrWhiteSpace(request.ThreadId)
            ? new AzureAIAgentThread(client)
            : new AzureAIAgentThread(client, request.ThreadId);

        var userMessage = new ChatMessageContent(AuthorRole.User, request.Message);
        var responseMessages = new List<string>();
        string? threadId = null;

        await foreach (var item in agent.InvokeAsync([userMessage], thread, cancellationToken: cancellationToken))
        {
            threadId ??= item.Thread?.Id;
            if (item.Message.Role == AuthorRole.Assistant && item.Message.Content is not null)
            {
                responseMessages.Add(item.Message.Content);
            }
        }

        return new ChatResponse
        {
            Message = string.Join(Environment.NewLine, responseMessages),
            ThreadId = threadId ?? thread.Id ?? string.Empty
        };
    }

    private async Task<(AzureAIAgent Agent, PersistentAgentsClient Client)> GetOrCreateAgentAsync(CancellationToken cancellationToken)
    {
        if (_agentState.HasValue)
        {
            return _agentState.Value;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_agentState.HasValue)
            {
                return _agentState.Value;
            }

            var endpoint = _configuration["AzureFoundry:Endpoint"]
                ?? throw new InvalidOperationException("AzureFoundry:Endpoint is not configured.");
            var modelDeployment = _configuration["AzureFoundry:ModelDeploymentName"]
                ?? throw new InvalidOperationException("AzureFoundry:ModelDeploymentName is not configured.");
            var agentName = _configuration["AzureFoundry:AgentName"] ?? "ChatAgent";
            var agentInstructions = _configuration["AzureFoundry:AgentInstructions"] ?? "You are a helpful assistant.";

            var credential = new DefaultAzureCredential();
            var client = AzureAIAgent.CreateAgentsClient(endpoint, credential);
            var adminClient = new PersistentAgentsAdministrationClient(endpoint, credential);

            var persistentAgent = await GetOrCreatePersistentAgentAsync(
                adminClient, modelDeployment, agentName, agentInstructions, cancellationToken);

            _persistentAgentId = persistentAgent.Id;
            _agentState = (new AzureAIAgent(persistentAgent, client), client);

            _logger.LogInformation("Using agent '{AgentId}' ('{AgentName}').", persistentAgent.Id, persistentAgent.Name);

            return _agentState.Value;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<PersistentAgent> GetOrCreatePersistentAgentAsync(
        PersistentAgentsAdministrationClient adminClient,
        string modelDeployment,
        string agentName,
        string agentInstructions,
        CancellationToken cancellationToken)
    {
        // Check for an existing agent with the same name to avoid accumulating orphaned agents.
        await foreach (var existingAgent in adminClient.GetAgentsAsync(cancellationToken: cancellationToken))
        {
            if (string.Equals(existingAgent.Name, agentName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Reusing existing agent '{AgentId}' ('{AgentName}').", existingAgent.Id, existingAgent.Name);
                return existingAgent;
            }
        }

        _logger.LogInformation("Creating Azure AI Foundry agent '{AgentName}' with model '{Model}'.", agentName, modelDeployment);

        var createResponse = await adminClient.CreateAgentAsync(
            model: modelDeployment,
            name: agentName,
            description: "A basic chat agent powered by Azure AI Foundry.",
            instructions: agentInstructions,
            cancellationToken: cancellationToken);

        return createResponse.Value;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_persistentAgentId is not null)
        {
            try
            {
                var endpoint = _configuration["AzureFoundry:Endpoint"];
                if (!string.IsNullOrEmpty(endpoint))
                {
                    var credential = new DefaultAzureCredential();
                    var adminClient = new PersistentAgentsAdministrationClient(endpoint, credential);
                    await adminClient.DeleteAgentAsync(_persistentAgentId);
                    _logger.LogInformation("Agent '{AgentId}' deleted on shutdown.", _persistentAgentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete agent on shutdown.");
            }
        }

        _initLock.Dispose();
    }
}
