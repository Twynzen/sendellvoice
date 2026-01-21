# SendellVoice

An intelligent contact center system that combines voice processing, AI-powered intent classification, and Retrieval-Augmented Generation (RAG) for contextual responses. Built with .NET 8 and Microsoft Semantic Kernel.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          SENDELLVOICE ARCHITECTURE                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  PRESENTATION LAYER                                                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │  REST API   │  │  WebSocket  │  │  Webhooks   │  │  Health     │        │
│  │  Controllers│  │  SignalR    │  │  Callbacks  │  │  Endpoints  │        │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────────────┘        │
├─────────┴────────────────┴────────────────┴─────────────────────────────────┤
│  APPLICATION LAYER (Semantic Kernel Orchestration)                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ Intent          │  │ Speech-to-      │  │ RAG Knowledge   │              │
│  │ Classification  │  │ Action Pipeline │  │ Base Manager    │              │
│  │ Plugin          │  │ Service         │  │ Plugin          │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
├───────────┴────────────────────┴────────────────────┴───────────────────────┤
│  INFRASTRUCTURE LAYER                                                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ LLM Provider │  │ Speech       │  │ Vector Store │  │ SQL Server   │     │
│  │ (Azure/      │  │ (Azure/      │  │ (Azure/      │  │ (EF Core)    │     │
│  │  Ollama)     │  │  Whisper)    │  │  Qdrant)     │  │              │     │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Features

- **Smart Intent Classification**: AI-powered classification of customer intents (Billing, Technical, Account, Complaint, Sales, etc.)
- **Speech-to-Action Pipeline**: Convert audio to text and automatically determine appropriate actions
- **RAG Knowledge Base**: Upload documents and query them with natural language for contextual responses
- **Multiple Provider Support**: Switch between Azure services and local alternatives (Ollama, Whisper.NET, Qdrant)
- **Clean Architecture**: Domain, Application, Infrastructure, and Web layers with clear separation of concerns

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Ollama](https://ollama.ai) (for local LLM)

### Run with Docker Compose

```bash
# Start all services (SQL Server, Qdrant, Ollama, API)
docker-compose up -d

# Pull required Ollama models
docker exec sendellvoice-ollama ollama pull llama3.2
docker exec sendellvoice-ollama ollama pull nomic-embed-text

# The API will be available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Run Locally (Development)

```bash
# Start dependencies only
docker-compose up -d sqlserver qdrant ollama

# Pull Ollama models
ollama pull llama3.2
ollama pull nomic-embed-text

# Run the API
cd src/SendellVoice.Web
dotnet run
```

## API Endpoints

### Conversations
- `POST /api/conversations` - Create a new conversation
- `GET /api/conversations/{id}` - Get conversation details
- `POST /api/conversations/{id}/messages` - Send message and get AI response
- `GET /api/conversations/{id}/messages` - Get conversation history
- `GET /api/conversations/active` - Get active conversations
- `POST /api/conversations/{id}/end` - End a conversation

### Speech
- `POST /api/speech/transcribe` - Transcribe audio to text
- `POST /api/speech/process` - Process audio and determine action
- `GET /api/speech/health` - Check speech service status

### Knowledge Base
- `POST /api/knowledgebase/documents` - Upload and ingest a document
- `POST /api/knowledgebase/query` - Query the knowledge base
- `GET /api/knowledgebase/documents` - List all documents
- `DELETE /api/knowledgebase/documents/{id}` - Delete a document

### Health
- `GET /health` - Full health check
- `GET /health/ready` - Readiness check
- `GET /health/live` - Liveness check

## Configuration

### Provider Selection

Configure service providers in `appsettings.json`:

```json
{
  "Providers": {
    "LLM": "Ollama",          // Options: Ollama, AzureOpenAI
    "Speech": "WhisperNet",   // Options: WhisperNet, AzureSpeech
    "VectorStore": "InMemory" // Options: InMemory, Qdrant, AzureAISearch
  }
}
```

### Cost Comparison

| Service | Azure (Paid) | Local Alternative (Free) |
|---------|--------------|-------------------------|
| **LLM Chat** | Azure OpenAI GPT-4o-mini: $0.15-0.60/1M tokens | Ollama + Llama 3.2: $0 |
| **Embeddings** | text-embedding-3-small: $0.02/1M tokens | Ollama + nomic-embed: $0 |
| **Speech-to-Text** | Azure Speech: $1/hour | Whisper.NET: $0 |
| **Vector Search** | Azure AI Search: $73/month | Qdrant/InMemory: $0 |

## Project Structure

```
SendellVoice/
├── src/
│   ├── SendellVoice.Domain/          # Entities, Enums, Interfaces
│   ├── SendellVoice.Application/     # Services, Plugins, Business Logic
│   ├── SendellVoice.Infrastructure/  # External Service Implementations
│   └── SendellVoice.Web/             # API Controllers, Middleware
├── tests/
│   ├── SendellVoice.UnitTests/
│   └── SendellVoice.IntegrationTests/
├── docker-compose.yml
├── Dockerfile
└── SendellVoice.sln
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | - |
| `Providers__LLM` | LLM provider (Ollama/AzureOpenAI) | Ollama |
| `Providers__Speech` | Speech provider (WhisperNet/AzureSpeech) | WhisperNet |
| `Providers__VectorStore` | Vector store (InMemory/Qdrant/AzureAISearch) | InMemory |
| `Ollama__Endpoint` | Ollama API endpoint | http://localhost:11434 |
| `Qdrant__Host` | Qdrant host | localhost |
| `ApiKey` | Optional API key for authentication | - |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Ollama not responding | Run `ollama serve` or check Docker container |
| "Model not found" error | Run `ollama pull llama3.2` |
| SQL Server connection refused | Wait for healthcheck or verify password |
| Whisper very slow | Use smaller model (tiny/base) or enable GPU |

## License

MIT License
