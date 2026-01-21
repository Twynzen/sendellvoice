using Microsoft.EntityFrameworkCore;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for KnowledgeDocument entity.
/// </summary>
public class KnowledgeRepository : BaseRepository<KnowledgeDocument>, IKnowledgeRepository
{
    public KnowledgeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.Category == category && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var tagList = tags.ToList();

        return await DbSet
            .Where(d => d.IsActive && d.Tags.Any(t => tagList.Contains(t)))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => !d.IsProcessed)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetActiveDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.IsActive && d.IsProcessed)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<KnowledgeDocument?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.FileName == fileName, cancellationToken);
    }
}
