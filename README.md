# msft-agent-framework-aca-demo
Microsoft Agent Framework example API

## Overview

A .NET 8 Web API that implements a basic AI chat agent using the **Microsoft Semantic Kernel Agent Framework** backed by **Azure AI Foundry** (Microsoft Foundry). The agent supports multi-turn conversations via thread IDs.

## Architecture

- **Framework:** ASP.NET Core 8 Web API
- **Agent Framework:** [Microsoft Semantic Kernel Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/) (`Microsoft.SemanticKernel.Agents.AzureAI`)
- **AI Service:** [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-studio/) via the Azure AI Agent Service (`Azure.AI.Agents.Persistent`)
- **Authentication:** `DefaultAzureCredential` (supports managed identity, Azure CLI, etc.)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [Azure AI Foundry](https://ai.azure.com) project with a deployed model (e.g. `gpt-4o`)
- Azure credentials configured (Azure CLI login, managed identity, or environment variables)

## Configuration

Update `ChatAgent/appsettings.json` (or use environment variables / user secrets) with your Azure AI Foundry details:

```json
{
  "AzureFoundry": {
    "Endpoint": "https://<your-hub>.services.ai.azure.com/api/projects/<your-project>",
    "ModelDeploymentName": "gpt-4o",
    "AgentName": "BasicChatAgent",
    "AgentInstructions": "You are a helpful assistant. Answer questions clearly and concisely."
  }
}
```

| Setting | Description |
|---|---|
| `Endpoint` | Your Azure AI Foundry project endpoint |
| `ModelDeploymentName` | The name of your deployed model |
| `AgentName` | Display name for the agent (created in AI Foundry on startup) |
| `AgentInstructions` | System prompt / instructions for the agent |

## Running Locally

```bash
cd ChatAgent
dotnet run
```

The API will be available at `http://localhost:5121` with Swagger UI at `http://localhost:5121/swagger`.

## API

### `POST /chat`

Send a message to the agent. Optionally include a `threadId` to continue an existing conversation.

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
  "threadId": "thread_abc123"
}
```

Use the returned `threadId` in subsequent requests to continue the same conversation.

## Project Structure

```
ChatAgent/
├── Controllers/
│   └── ChatController.cs       # POST /chat endpoint
├── Models/
│   ├── ChatRequest.cs          # Request model
│   └── ChatResponse.cs         # Response model
├── Services/
│   ├── IChatAgentService.cs    # Service interface
│   └── ChatAgentService.cs     # Azure AI Foundry agent implementation
├── Program.cs                  # App configuration and DI
├── appsettings.json            # Configuration
└── ChatAgent.http              # HTTP test file
```
