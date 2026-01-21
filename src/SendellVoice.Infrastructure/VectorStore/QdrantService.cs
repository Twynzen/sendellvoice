using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.VectorStore;

/// <summary>
/// Qdrant-based implementation of vector store service.
/// </summary>
public class QdrantService : IVectorStoreService
{
    private readonly QdrantClient _client;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantService> _logger;
    private const string CollectionName = "sendellvoice-docs";
    private readonly int _vectorDimension;

    public QdrantService(
        IOptions<QdrantSettings> options,
        IEmbeddingService embeddingService,
        ILogger<QdrantService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _vectorDimension = embeddingService.GetDimensions();
        _client = new QdrantClient(_settings.Host, _settings.Port);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);

            if (!collections.Any(c => c == CollectionName))
            {
                _logger.LogInformation("Creating Qdrant collection: {Collection} with dimension: {Dimension}",
                    CollectionName, _vectorDimension);

                await _client.CreateCollectionAsync(
                    CollectionName,
                    new VectorParams
                    {
                        Size = (ulong)_vectorDimension,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Qdrant collection ready: {Collection}", CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Qdrant collection");
            throw;
        }
    }

    public async Task AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
    {
        if (chunk.Embedding == null)
        {
            throw new ArgumentException("Document chunk must have an embedding", nameof(chunk));
        }

        var point = CreatePoint(chunk);
        await _client.UpsertAsync(CollectionName, new[] { point }, cancellationToken: cancellationToken);

        _logger.LogDebug("Added document to Qdrant: {Id}", chunk.Id);
    }

    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var points = chunks.Select(CreatePoint).ToList();

        if (points.Count == 0) return;

        // Batch upsert in groups of 100
        const int batchSize = 100;
        for (int i = 0; i < points.Count; i += batchSize)
        {
            var batch = points.Skip(i).Take(batchSize);
            await _client.UpsertAsync(CollectionName, batch, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Added {Count} documents to Qdrant", points.Count);
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
        Filter? qdrantFilter = null;

        if (!string.IsNullOrEmpty(filter) && filter.StartsWith("category:"))
        {
            var category = filter.Substring("category:".Length);
            qdrantFilter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "category",
                            Match = new Match { Keyword = category }
                        }
                    }
                }
            };
        }

        var results = await _client.SearchAsync(
            CollectionName,
            queryVector,
            filter: qdrantFilter,
            limit: (ulong)topK,
            cancellationToken: cancellationToken);

        return results.Select(r => new SearchResult(
            r.Id.Uuid ?? r.Id.Num.ToString(),
            r.Payload.TryGetValue("content", out var content) ? content.StringValue : string.Empty,
            r.Score,
            r.Payload.TryGetValue("source_file", out var source) ? source.StringValue : null,
            r.Payload.TryGetValue("chunk_index", out var index) ? (int?)index.IntegerValue : null,
            r.Payload
                .Where(p => !new[] { "content", "source_file", "chunk_index" }.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value.StringValue)
        )).ToList();
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(documentId, out var guid))
        {
            await _client.DeleteAsync(
                CollectionName,
                new PointsSelector { Points = new PointsIdsList { Ids = { new PointId { Uuid = documentId } } } },
                cancellationToken: cancellationToken);
        }

        _logger.LogDebug("Deleted document from Qdrant: {Id}", documentId);
    }

    public async Task DeleteBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "source_file",
                        Match = new Match { Keyword = sourceFile }
                    }
                }
            }
        };

        await _client.DeleteAsync(
            CollectionName,
            new PointsSelector { Filter = filter },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Deleted documents from Qdrant with source: {Source}", sourceFile);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.ListCollectionsAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static PointStruct CreatePoint(DocumentChunk chunk)
    {
        var pointId = Guid.TryParse(chunk.Id, out var guid)
            ? new PointId { Uuid = guid.ToString() }
            : new PointId { Uuid = Guid.NewGuid().ToString() };

        var payload = new Dictionary<string, Value>
        {
            ["content"] = new Value { StringValue = chunk.Content },
            ["source_file"] = new Value { StringValue = chunk.SourceFile },
            ["chunk_index"] = new Value { IntegerValue = chunk.ChunkIndex }
        };

        foreach (var meta in chunk.Metadata)
        {
            payload[meta.Key] = new Value { StringValue = meta.Value };
        }

        return new PointStruct
        {
            Id = pointId,
            Vectors = chunk.Embedding!,
            Payload = { payload }
        };
    }
}
