namespace SendellVoice.Domain.Enums;

/// <summary>
/// Represents the current status of a conversation.
/// </summary>
public enum ConversationStatus
{
    Active = 0,
    OnHold = 1,
    Transferred = 2,
    Escalated = 3,
    Resolved = 4,
    Abandoned = 5,
    Closed = 6
}
