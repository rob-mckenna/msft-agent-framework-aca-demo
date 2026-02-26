# msft-agent-framework-aca-demo
Microsoft Agent Framework example API

## Overview

A .NET 8 Web API that implements a basic AI chat agent using the **[Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)** (`Microsoft.Agents.Hosting.AspNetCore`) with **Azure OpenAI**. The agent is exposed over the Bot Framework Activity Protocol and supports multi-turn conversations.

## Architecture

- **Framework:** ASP.NET Core 8 Web API
- **Agent Framework:** [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) (`Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.Builder`)
- **AI Service:** [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/) (`Azure.AI.OpenAI`)
- **Authentication:** `DefaultAzureCredential` (supports managed identity, Azure CLI, etc.)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [Azure OpenAI](https://portal.azure.com) resource with a deployed model (e.g. `gpt-4o`)
- Azure credentials configured (Azure CLI login, managed identity, or environment variables)

## Configuration

Update `ChatAgent/appsettings.json` (or use environment variables / user secrets):

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "SystemPrompt": "You are a helpful assistant. Answer questions clearly and concisely."
  }
}
```

| Setting | Description |
|---|---|
| `Endpoint` | Your Azure OpenAI resource endpoint |
| `DeploymentName` | The name of your model deployment |
| `SystemPrompt` | System instructions for the agent |

## Running Locally

```bash
cd ChatAgent
dotnet run
```

The API will be available at `http://localhost:5121` with Swagger UI at `http://localhost:5121/swagger`.

## API

### `POST /api/messages` — Agent Framework endpoint (Bot Framework Activity Protocol)

The primary endpoint for the Microsoft Agent Framework. Accepts Bot Framework Activity JSON from channels (Teams, Bot Framework Emulator, WebChat, etc.).

For local testing, use the [Bot Framework Emulator](https://github.com/microsoft/BotFramework-Emulator) and connect to `http://localhost:5121/api/messages`.

**Example activity:**
```json
{
  "type": "message",
  "id": "1",
  "from": { "id": "user1", "name": "User" },
  "conversation": { "id": "conv1" },
  "recipient": { "id": "agent", "name": "Agent" },
  "text": "Hello! What can you help me with today?"
}
```

---

### `POST /chat` — REST endpoint (simplified, for testing)

A simplified JSON endpoint for direct HTTP testing. Supports multi-turn conversations via `threadId`.

**Request:**
```json
{
  "message": "Hello! What can you help me with today?",
  "threadId": "optional-existing-thread-id"
}
```

**Response:**
```json
{
  "message": "Hello! I can help you with a wide range of tasks...",
  "threadId": "b3f2a1c4-..."
}
```

Use the returned `threadId` in subsequent requests to continue the same conversation.

## Project Structure

```
ChatAgent/
├── Agents/
│   └── BasicChatAgent.cs       # AgentApplication subclass (Agent Framework)
├── Controllers/
│   └── ChatController.cs       # POST /chat REST endpoint
├── Models/
│   ├── ChatRequest.cs          # Request model
│   └── ChatResponse.cs         # Response model
├── Services/
│   ├── IChatAgentService.cs    # Service interface
│   └── ChatAgentService.cs     # Azure OpenAI-backed implementation
├── Program.cs                  # App configuration and DI
├── appsettings.json            # Configuration
└── ChatAgent.http              # HTTP test file
```
