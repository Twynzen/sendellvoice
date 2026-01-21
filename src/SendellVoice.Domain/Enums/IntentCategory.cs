namespace SendellVoice.Domain.Enums;

/// <summary>
/// Represents the classified intent category of a customer message.
/// </summary>
public enum IntentCategory
{
    Unknown = 0,
    Billing = 1,
    Technical = 2,
    Account = 3,
    Complaint = 4,
    Sales = 5,
    General = 6,
    Cancellation = 7,
    Feedback = 8,
    Emergency = 9
}
