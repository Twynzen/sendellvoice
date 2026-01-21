using SendellVoice.Domain.Enums;

namespace SendellVoice.Domain.Entities;

/// <summary>
/// Represents a classified intent from a customer message.
/// </summary>
public class Intent : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid? MessageId { get; set; }
    public Message? Message { get; set; }

    public IntentCategory Category { get; set; }
    public double Confidence { get; set; }
    public string? Reasoning { get; set; }

    public string? OriginalText { get; set; }
    public List<string> ExtractedEntities { get; set; } = new();

    public string? SuggestedAction { get; set; }
    public bool WasActionTaken { get; set; }
}
