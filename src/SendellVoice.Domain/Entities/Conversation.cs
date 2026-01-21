using SendellVoice.Domain.Enums;

namespace SendellVoice.Domain.Entities;

/// <summary>
/// Represents a conversation between a customer and the contact center.
/// </summary>
public class Conversation : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public ChannelType Channel { get; set; }
    public ConversationStatus Status { get; set; }

    public Guid? AssignedAgentId { get; set; }
    public Agent? AssignedAgent { get; set; }

    public IntentCategory? PrimaryIntent { get; set; }
    public double? IntentConfidence { get; set; }

    public string? Summary { get; set; }
    public string? Resolution { get; set; }

    public DateTime? EndedAt { get; set; }
    public int? SatisfactionScore { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();

    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Intent> Intents { get; set; } = new List<Intent>();
}
