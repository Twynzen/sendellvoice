using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;

namespace SendellVoice.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin for retrieving knowledge from the RAG system.
/// </summary>
public class KnowledgeRetrievalPlugin
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<KnowledgeRetrievalPlugin> _logger;

    public KnowledgeRetrievalPlugin(
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<KnowledgeRetrievalPlugin> logger)
    {
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    [KernelFunction("search_knowledge")]
    [Description("Searches the knowledge base for information relevant to the query")]
    public async Task<IReadOnlyList<SearchResult>> SearchKnowledgeAsync(
        [Description("The search query")] string query,
        [Description("Maximum number of results to return")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching knowledge base for: {QueryPreview}",
            query.Length > 50 ? query[..50] + "..." : query);

        var embedding = await _embeddingService.GenerateAsync(query, cancellationToken);
        var results = await _vectorStoreService.SearchAsync(embedding, topK, cancellationToken);

        _logger.LogInformation("Found {Count} results for knowledge search", results.Count);

        return results;
    }

    [KernelFunction("search_knowledge_with_filter")]
    [Description("Searches the knowledge base with a category filter")]
    public async Task<IReadOnlyList<SearchResult>> SearchKnowledgeWithFilterAsync(
        [Description("The search query")] string query,
        [Description("Category filter")] string? category,
        [Description("Maximum number of results to return")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching knowledge base for: {Query} with category: {Category}", query, category);

        var embedding = await _embeddingService.GenerateAsync(query, cancellationToken);
        var filter = !string.IsNullOrEmpty(category) ? $"category:{category}" : null;
        var results = await _vectorStoreService.SearchAsync(embedding, filter, topK, cancellationToken);

        return results;
    }

    [KernelFunction("get_formatted_context")]
    [Description("Retrieves and formats knowledge as context for response generation")]
    public async Task<string> GetFormattedContextAsync(
        [Description("The query to search for")] string query,
        [Description("Maximum number of documents to include")] int maxDocuments = 3,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchKnowledgeAsync(query, maxDocuments, cancellationToken);

        if (!results.Any())
        {
            _logger.LogInformation("No relevant knowledge found for query");
            return string.Empty;
        }

        var contextBuilder = new StringBuilder();

        foreach (var result in results)
        {
            contextBuilder.AppendLine($"--- Source: {result.SourceFile ?? "Unknown"} (Relevance: {result.Score:P0}) ---");
            contextBuilder.AppendLine(result.Content);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    [KernelFunction("check_knowledge_exists")]
    [Description("Checks if relevant knowledge exists for a query")]
    public async Task<bool> CheckKnowledgeExistsAsync(
        [Description("The query to check")] string query,
        [Description("Minimum relevance score threshold")] double threshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchKnowledgeAsync(query, 1, cancellationToken);

        return results.Any() && results[0].Score >= threshold;
    }
}
