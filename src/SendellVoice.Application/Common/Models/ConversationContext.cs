using SendellVoice.Domain.Enums;

namespace SendellVoice.Application.Common.Models;

/// <summary>
/// Context information for a conversation used in response generation.
/// </summary>
public record ConversationContext
{
    public Guid ConversationId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public ChannelType Channel { get; init; }
    public IntentCategory? CurrentIntent { get; init; }
    public IReadOnlyList<MessageContext> RecentMessages { get; init; } = new List<MessageContext>();
    public IReadOnlyList<SearchResult> RelevantKnowledge { get; init; } = new List<SearchResult>();
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Context for a single message in a conversation.
/// </summary>
public record MessageContext
{
    public string Content { get; init; } = string.Empty;
    public MessageDirection Direction { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsFromBot { get; init; }
}
