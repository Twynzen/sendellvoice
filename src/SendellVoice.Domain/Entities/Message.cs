using SendellVoice.Domain.Enums;

namespace SendellVoice.Domain.Entities;

/// <summary>
/// Represents a single message within a conversation.
/// </summary>
public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public MessageType Type { get; set; }

    public string? SenderName { get; set; }
    public string? SenderId { get; set; }

    // For audio messages
    public string? AudioUrl { get; set; }
    public TimeSpan? AudioDuration { get; set; }
    public string? Transcription { get; set; }

    // AI analysis
    public IntentCategory? DetectedIntent { get; set; }
    public double? IntentConfidence { get; set; }
    public string? Sentiment { get; set; }
    public double? SentimentScore { get; set; }

    public bool IsFromBot { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
