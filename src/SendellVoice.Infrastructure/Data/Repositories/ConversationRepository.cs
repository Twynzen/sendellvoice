using Microsoft.EntityFrameworkCore;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Enums;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Conversation entity.
/// </summary>
public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Include(c => c.Intents)
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByStatusAsync(ConversationStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.AssignedAgentId == agentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == ConversationStatus.Active ||
                        c.Status == ConversationStatus.OnHold ||
                        c.Status == ConversationStatus.Escalated)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetRecentConversationsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
