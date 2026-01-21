using Microsoft.EntityFrameworkCore;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Message entity.
/// </summary>
public class MessageRepository : BaseRepository<Message>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetRecentMessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
