namespace SendellVoice.Domain.Entities;

/// <summary>
/// Represents a contact center agent who handles customer conversations.
/// </summary>
public class Agent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Skills { get; set; } = new();
    public string? Department { get; set; }

    // Navigation properties
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
