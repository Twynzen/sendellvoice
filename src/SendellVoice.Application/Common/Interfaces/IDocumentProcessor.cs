using SendellVoice.Application.Common.Models;

namespace SendellVoice.Application.Common.Interfaces;

/// <summary>
/// Interface for document processing and text extraction.
/// </summary>
public interface IDocumentProcessor
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string> ExtractTextAsync(Stream stream, string fileType, CancellationToken cancellationToken = default);
    IReadOnlyList<DocumentChunk> ChunkText(string text, string sourceFile, int chunkSize = 512, int overlap = 100);
    bool IsSupported(string fileExtension);
    IReadOnlyList<string> SupportedExtensions { get; }
}
