using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.VectorStore;

/// <summary>
/// Azure AI Search-based implementation of vector store service.
/// </summary>
public class AzureAISearchService : IVectorStoreService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly AzureAISearchSettings _settings;
    private readonly ILogger<AzureAISearchService> _logger;
    private readonly int _vectorDimension;

    public AzureAISearchService(
        IOptions<AzureAISearchSettings> options,
        IEmbeddingService embeddingService,
        ILogger<AzureAISearchService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _vectorDimension = embeddingService.GetDimensions();

        var credential = new AzureKeyCredential(_settings.ApiKey);
        _indexClient = new SearchIndexClient(new Uri(_settings.Endpoint), credential);
        _searchClient = new SearchClient(new Uri(_settings.Endpoint), _settings.IndexName, credential);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexes = _indexClient.GetIndexesAsync(cancellationToken);
            var indexExists = false;

            await foreach (var index in indexes)
            {
                if (index.Name == _settings.IndexName)
                {
                    indexExists = true;
                    break;
                }
            }

            if (!indexExists)
            {
                _logger.LogInformation("Creating Azure AI Search index: {Index}", _settings.IndexName);

                var searchIndex = new SearchIndex(_settings.IndexName)
                {
                    Fields =
                    {
                        new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                        new SearchableField("content") { IsFilterable = false },
                        new SimpleField("sourceFile", SearchFieldDataType.String) { IsFilterable = true },
                        new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsFilterable = true },
                        new SimpleField("category", SearchFieldDataType.String) { IsFilterable = true },
                        new VectorSearchField("embedding", _vectorDimension, "vectorConfig")
                    },
                    VectorSearch = new VectorSearch
                    {
                        Profiles =
                        {
                            new VectorSearchProfile("vectorProfile", "vectorConfig")
                        },
                        Algorithms =
                        {
                            new HnswAlgorithmConfiguration("vectorConfig")
                            {
                                Parameters = new HnswParameters
                                {
                                    Metric = VectorSearchAlgorithmMetric.Cosine,
                                    M = 4,
                                    EfConstruction = 400,
                                    EfSearch = 500
                                }
                            }
                        }
                    }
                };

                await _indexClient.CreateIndexAsync(searchIndex, cancellationToken);
            }

            _logger.LogInformation("Azure AI Search index ready: {Index}", _settings.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Search index");
            throw;
        }
    }

    public async Task AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
    {
        if (chunk.Embedding == null)
        {
            throw new ArgumentException("Document chunk must have an embedding", nameof(chunk));
        }

        var document = CreateSearchDocument(chunk);
        await _searchClient.MergeOrUploadDocumentsAsync(new[] { document }, cancellationToken: cancellationToken);

        _logger.LogDebug("Added document to Azure AI Search: {Id}", chunk.Id);
    }

    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var documents = chunks.Select(CreateSearchDocument).ToList();

        if (documents.Count == 0) return;

        // Batch upload in groups of 1000 (Azure AI Search limit)
        const int batchSize = 1000;
        for (int i = 0; i < documents.Count; i += batchSize)
        {
            var batch = documents.Skip(i).Take(batchSize);
            await _searchClient.MergeOrUploadDocumentsAsync(batch, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Added {Count} documents to Azure AI Search", documents.Count);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(queryVector, null, topK, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        string? filter,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        // Validar topK para prevenir DoS
        topK = Math.Clamp(topK, 1, 100);

        string? azureFilter = null;

        if (!string.IsNullOrEmpty(filter) && filter.StartsWith("category:"))
        {
            var category = filter["category:".Length..];
            // Sanitizar para prevenir inyección OData - solo permitir caracteres alfanuméricos y espacios
            category = SanitizeODataValue(category);
            if (!string.IsNullOrEmpty(category))
            {
                azureFilter = $"category eq '{category}'";
            }
        }

        var searchOptions = new SearchOptions
        {
            Size = topK,
            Filter = azureFilter,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryVector.Select(f => (float)f).ToArray())
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "embedding" }
                    }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions, cancellationToken);

        var results = new List<SearchResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(new SearchResult(
                result.Document.GetString("id"),
                result.Document.GetString("content"),
                result.Score ?? 0,
                result.Document.GetString("sourceFile"),
                result.Document.GetInt32("chunkIndex"),
                new Dictionary<string, string>
                {
                    ["category"] = result.Document.GetString("category") ?? string.Empty
                }));
        }

        return results;
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        await _searchClient.DeleteDocumentsAsync("id", new[] { documentId }, cancellationToken: cancellationToken);
        _logger.LogDebug("Deleted document from Azure AI Search: {Id}", documentId);
    }

    public async Task DeleteBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile, nameof(sourceFile));

        // Sanitizar sourceFile para prevenir inyección OData
        var sanitizedSourceFile = SanitizeODataValue(sourceFile);
        if (string.IsNullOrEmpty(sanitizedSourceFile))
        {
            _logger.LogWarning("Invalid sourceFile provided for deletion: {SourceFile}", sourceFile);
            return;
        }

        // Search for all documents with the source file
        var searchOptions = new SearchOptions
        {
            Filter = $"sourceFile eq '{sanitizedSourceFile}'",
            Size = 1000,
            Select = { "id" }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions, cancellationToken);

        var idsToDelete = new List<string>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            idsToDelete.Add(result.Document.GetString("id"));
        }

        if (idsToDelete.Count > 0)
        {
            await _searchClient.DeleteDocumentsAsync("id", idsToDelete, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Deleted {Count} documents from Azure AI Search with source: {Source}",
            idsToDelete.Count, sourceFile);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _indexClient.GetIndexAsync(_settings.IndexName, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static SearchDocument CreateSearchDocument(DocumentChunk chunk)
    {
        var doc = new SearchDocument
        {
            ["id"] = chunk.Id,
            ["content"] = chunk.Content,
            ["sourceFile"] = chunk.SourceFile,
            ["chunkIndex"] = chunk.ChunkIndex,
            ["embedding"] = chunk.Embedding!.Select(f => (float)f).ToArray()
        };

        if (chunk.Metadata.TryGetValue("category", out var category))
        {
            doc["category"] = category;
        }

        return doc;
    }

    /// <summary>
    /// Sanitiza un valor para uso seguro en filtros OData.
    /// Previene ataques de inyección OData eliminando caracteres peligrosos.
    /// </summary>
    private static string SanitizeODataValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Escapar comillas simples duplicándolas (estándar OData)
        var sanitized = value.Replace("'", "''");

        // Remover caracteres de control y caracteres potencialmente peligrosos
        sanitized = new string(sanitized
            .Where(c => !char.IsControl(c) && c != '\'' || c == ' ')
            .ToArray());

        // Limitar longitud para prevenir DoS
        const int maxLength = 256;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized.Trim();
    }
}
