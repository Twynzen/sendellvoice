namespace SendellVoice.Application.Common.Models;

/// <summary>
/// A chunk of a processed document for vector storage.
/// </summary>
public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public float[]? Embedding { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
