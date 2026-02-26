using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using OpenAI.Chat;

namespace ChatAgent.Agents;

/// <summary>
/// A basic chat agent built with the Microsoft Agent Framework.
/// Handles incoming message activities and replies using Azure OpenAI.
/// The agent is exposed over the Bot Framework Activity Protocol at POST /api/messages.
/// </summary>
public class BasicChatAgent : AgentApplication
{
    // Key used to store conversation history in the Agent Framework conversation state.
    private const string HistoryKey = "conversation.chatHistory";

    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;

    public BasicChatAgent(AgentApplicationOptions options, IConfiguration configuration)
        : base(options)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var deployment = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        _systemPrompt = configuration["AzureOpenAI:SystemPrompt"] ?? "You are a helpful assistant.";

        var credential = new DefaultAzureCredential();
        _chatClient = new AzureOpenAIClient(new Uri(endpoint), credential).GetChatClient(deployment);

        // Handle new members joining the conversation
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, OnMembersAddedAsync);

        // Handle all incoming messages (Regex ".*" matches any message text)
        OnMessage(new Regex(".*"), OnMessageAsync);
    }

    private static async Task OnMembersAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (var member in turnContext.Activity.MembersAdded ?? [])
        {
            if (member.Id != turnContext.Activity.Recipient?.Id)
            {
                await turnContext.SendActivityAsync(
                    "Hello! I'm an AI chat agent powered by Azure OpenAI. How can I help you today?",
                    cancellationToken: cancellationToken);
            }
        }
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var userMessage = turnContext.Activity.Text;

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        // Retrieve persisted conversation history from Agent Framework state (backed by IStorage).
        var history = turnState.GetValue<List<ConversationEntry>>(HistoryKey, () => []);

        // Build the message list: system prompt + history + new user message.
        var messages = new List<ChatMessage> { new SystemChatMessage(_systemPrompt) };
        foreach (var entry in history)
        {
            messages.Add(entry.Role == "user"
                ? new UserChatMessage(entry.Content)
                : (ChatMessage)new AssistantChatMessage(entry.Content));
        }
        messages.Add(new UserChatMessage(userMessage));

        var result = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var reply = result.Value.Content[0].Text;

        // Persist the new exchange to conversation state.
        history.Add(new ConversationEntry("user", userMessage));
        history.Add(new ConversationEntry("assistant", reply));
        turnState.SetValue(HistoryKey, history);

        await turnContext.SendActivityAsync(reply, cancellationToken: cancellationToken);
    }

    /// <summary>Simple serialisable record for storing a single chat message in state.</summary>
    private sealed record ConversationEntry(string Role, string Content);
}
