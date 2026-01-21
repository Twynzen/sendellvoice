using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.AI;

/// <summary>
/// Azure OpenAI-based implementation of LLM and embedding services.
/// </summary>
public class AzureOpenAIService : ILlmService, IEmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(IOptions<AzureOpenAISettings> options, ILogger<AzureOpenAIService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new ApiKeyCredential(_settings.ApiKey));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating response with Azure OpenAI deployment: {Deployment}", _settings.ChatDeployment);

        var chatClient = _client.GetChatClient(_settings.ChatDeployment);
        var messages = new List<ChatMessage>
        {
            new UserChatMessage(prompt)
        };

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_settings.ChatDeployment);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<TResult> GenerateStructuredResponseAsync<TResult>(string prompt, CancellationToken cancellationToken = default)
        where TResult : class
    {
        var response = await GenerateResponseAsync(prompt, cancellationToken);

        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            return JsonSerializer.Deserialize<TResult>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to deserialize response");
        }

        throw new InvalidOperationException("Response does not contain valid JSON");
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_settings.ChatDeployment);
        var messages = new List<ChatMessage>
        {
            new UserChatMessage(prompt)
        };

        await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            foreach (var content in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(content.Text))
                {
                    yield return content.Text;
                }
            }
        }
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding with deployment: {Deployment}", _settings.EmbeddingDeployment);

        var embeddingClient = _client.GetEmbeddingClient(_settings.EmbeddingDeployment);
        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);

        return response.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogDebug("Generating {Count} embeddings", textList.Count);

        var embeddingClient = _client.GetEmbeddingClient(_settings.EmbeddingDeployment);
        var response = await embeddingClient.GenerateEmbeddingsAsync(textList, cancellationToken: cancellationToken);

        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }

    public int GetDimensions()
    {
        // text-embedding-3-small uses 1536 dimensions
        // text-embedding-3-large uses 3072 dimensions
        return _settings.EmbeddingDeployment switch
        {
            var d when d.Contains("small", StringComparison.OrdinalIgnoreCase) => 1536,
            var d when d.Contains("large", StringComparison.OrdinalIgnoreCase) => 3072,
            _ => 1536 // Default
        };
    }
}
