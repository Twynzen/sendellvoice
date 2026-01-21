namespace SendellVoice.Infrastructure.Configuration;

/// <summary>
/// Configuration for selecting service providers.
/// </summary>
public class ProviderSettings
{
    public string LLM { get; set; } = "Ollama";
    public string Speech { get; set; } = "WhisperNet";
    public string VectorStore { get; set; } = "InMemory";
}

/// <summary>
/// Azure OpenAI configuration settings.
/// </summary>
public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeployment { get; set; } = "gpt-4o-mini";
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
}

/// <summary>
/// Ollama configuration settings.
/// </summary>
public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}

/// <summary>
/// Azure Speech Services configuration settings.
/// </summary>
public class AzureSpeechSettings
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = "eastus";
    public string? Language { get; set; } = "en-US";
}

/// <summary>
/// Whisper.NET configuration settings.
/// </summary>
public class WhisperSettings
{
    public string ModelPath { get; set; } = "./models/ggml-base.bin";
    public string Language { get; set; } = "auto";
}

/// <summary>
/// Azure AI Search configuration settings.
/// </summary>
public class AzureAISearchSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "sendellvoice-docs";
}

/// <summary>
/// Qdrant configuration settings.
/// </summary>
public class QdrantSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
}
