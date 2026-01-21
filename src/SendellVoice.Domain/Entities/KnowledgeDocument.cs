namespace SendellVoice.Domain.Entities;

/// <summary>
/// Represents a document in the knowledge base for RAG.
/// </summary>
public class KnowledgeDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();

    public int ChunkCount { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Version { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();
}
