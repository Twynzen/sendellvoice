using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.AI;

/// <summary>
/// Ollama-based implementation of LLM and embedding services.
/// </summary>
public class OllamaService : ILlmService, IEmbeddingService, IDisposable
{
    private readonly OllamaApiClient _client;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(IOptions<OllamaSettings> options, ILogger<OllamaService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _client = new OllamaApiClient(_settings.Endpoint);
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating response with Ollama model: {Model}", _settings.Model);

        var response = await _client.GenerateAsync(new GenerateRequest
        {
            Model = _settings.Model,
            Prompt = prompt,
            Stream = false
        }, cancellationToken).ToListAsync(cancellationToken);

        return string.Join("", response.Select(r => r.Response));
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userMessage }
        };

        var response = await _client.ChatAsync(new ChatRequest
        {
            Model = _settings.Model,
            Messages = messages,
            Stream = false
        }, cancellationToken).ToListAsync(cancellationToken);

        return response.LastOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async Task<TResult> GenerateStructuredResponseAsync<TResult>(string prompt, CancellationToken cancellationToken = default)
        where TResult : class
    {
        var response = await GenerateResponseAsync(prompt, cancellationToken);

        // Try to extract JSON from the response
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
        await foreach (var response in _client.GenerateAsync(new GenerateRequest
        {
            Model = _settings.Model,
            Prompt = prompt,
            Stream = true
        }, cancellationToken))
        {
            if (!string.IsNullOrEmpty(response.Response))
            {
                yield return response.Response;
            }
        }
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding with model: {Model}", _settings.EmbeddingModel);

        var response = await _client.EmbedAsync(new EmbedRequest
        {
            Model = _settings.EmbeddingModel,
            Input = new[] { text }
        }, cancellationToken);

        return response.Embeddings?.FirstOrDefault()?.Select(d => (float)d).ToArray()
            ?? throw new InvalidOperationException("Failed to generate embedding");
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogDebug("Generating {Count} embeddings", textList.Count);

        var response = await _client.EmbedAsync(new EmbedRequest
        {
            Model = _settings.EmbeddingModel,
            Input = textList
        }, cancellationToken);

        return response.Embeddings?.Select(e => e.Select(d => (float)d).ToArray()).ToList()
            ?? throw new InvalidOperationException("Failed to generate embeddings");
    }

    public int GetDimensions()
    {
        // nomic-embed-text uses 768 dimensions
        // Most other models use 1536 or 4096
        return _settings.EmbeddingModel switch
        {
            "nomic-embed-text" => 768,
            "mxbai-embed-large" => 1024,
            "all-minilm" => 384,
            _ => 768 // Default
        };
    }

    public void Dispose()
    {
        // OllamaApiClient doesn't need explicit disposal
        GC.SuppressFinalize(this);
    }
}
