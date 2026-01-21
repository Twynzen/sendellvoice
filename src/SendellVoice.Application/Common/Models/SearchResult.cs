namespace SendellVoice.Application.Common.Models;

/// <summary>
/// Result of a vector similarity search.
/// </summary>
public record SearchResult(
    string Id,
    string Content,
    double Score,
    string? SourceFile = null,
    int? ChunkIndex = null,
    Dictionary<string, string>? Metadata = null
);
