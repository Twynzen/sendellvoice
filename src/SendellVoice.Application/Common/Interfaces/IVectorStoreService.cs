using SendellVoice.Application.Common.Models;

namespace SendellVoice.Application.Common.Interfaces;

/// <summary>
/// Interface for vector storage and similarity search services.
/// </summary>
public interface IVectorStoreService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default);
    Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryVector, string? filter, int topK = 5, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task DeleteBySourceAsync(string sourceFile, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
