using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Enums;

namespace SendellVoice.Domain.Interfaces;

/// <summary>
/// Repository interface for Conversation entity operations.
/// </summary>
public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetByStatusAsync(ConversationStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetRecentConversationsAsync(int count, CancellationToken cancellationToken = default);
}
