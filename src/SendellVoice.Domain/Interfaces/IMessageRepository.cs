using SendellVoice.Domain.Entities;

namespace SendellVoice.Domain.Interfaces;

/// <summary>
/// Repository interface for Message entity operations.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    Task<IReadOnlyList<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Message>> GetRecentMessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken = default);
}
