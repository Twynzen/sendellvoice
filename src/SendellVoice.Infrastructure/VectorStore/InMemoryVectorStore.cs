using Microsoft.Extensions.Logging;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;

namespace SendellVoice.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of vector store for development and testing.
/// </summary>
public class InMemoryVectorStore : IVectorStoreService
{
    private readonly List<VectorDocument> _documents = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<InMemoryVectorStore> _logger;

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("InMemoryVectorStore initialized");
        return Task.CompletedTask;
    }

    public Task AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
    {
        if (chunk.Embedding == null)
        {
            throw new ArgumentException("Document chunk must have an embedding", nameof(chunk));
        }

        _lock.EnterWriteLock();
        try
        {
            _documents.Add(new VectorDocument(
                chunk.Id,
                chunk.Content,
                chunk.Embedding,
                chunk.SourceFile,
                chunk.ChunkIndex,
                chunk.Metadata));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _logger.LogDebug("Added document: {Id}", chunk.Id);
        return Task.CompletedTask;
    }

    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            await AddDocumentAsync(chunk, cancellationToken);
        }

        _logger.LogInformation("Added {Count} documents to vector store", _documents.Count);
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(queryVector, null, topK, cancellationToken);
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        string? filter,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = _documents.AsEnumerable();

            // Apply filter if provided (simple category filter)
            if (!string.IsNullOrEmpty(filter) && filter.StartsWith("category:"))
            {
                var category = filter.Substring("category:".Length);
                candidates = candidates.Where(d =>
                    d.Metadata.TryGetValue("category", out var cat) &&
                    cat.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            var results = candidates
                .Select(doc => (Doc: doc, Score: CosineSimilarity(queryVector, doc.Vector)))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => new SearchResult(
                    x.Doc.Id,
                    x.Doc.Content,
                    x.Score,
                    x.Doc.SourceFile,
                    x.Doc.ChunkIndex,
                    x.Doc.Metadata))
                .ToList();

            _logger.LogDebug("Search returned {Count} results", results.Count);

            return Task.FromResult<IReadOnlyList<SearchResult>>(results);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _documents.RemoveAll(d => d.Id == documentId);
            _logger.LogDebug("Removed {Count} documents with ID: {Id}", removed, documentId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task DeleteBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _documents.RemoveAll(d =>
                d.SourceFile.Equals(sourceFile, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Removed {Count} documents from source: {Source}", removed, sourceFile);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dot = 0, magA = 0, magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var magnitude = Math.Sqrt(magA) * Math.Sqrt(magB);

        return magnitude > 0 ? dot / magnitude : 0;
    }

    private record VectorDocument(
        string Id,
        string Content,
        float[] Vector,
        string SourceFile,
        int ChunkIndex,
        Dictionary<string, string> Metadata);
}
