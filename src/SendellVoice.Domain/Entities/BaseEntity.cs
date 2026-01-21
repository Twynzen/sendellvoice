namespace SendellVoice.Domain.Entities;

/// <summary>
/// Base entity class with common properties for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
