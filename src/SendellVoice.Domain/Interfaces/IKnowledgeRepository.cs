using SendellVoice.Domain.Entities;

namespace SendellVoice.Domain.Interfaces;

/// <summary>
/// Repository interface for KnowledgeDocument entity operations.
/// </summary>
public interface IKnowledgeRepository : IRepository<KnowledgeDocument>
{
    Task<IReadOnlyList<KnowledgeDocument>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetActiveDocumentsAsync(CancellationToken cancellationToken = default);
    Task<KnowledgeDocument?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
}
