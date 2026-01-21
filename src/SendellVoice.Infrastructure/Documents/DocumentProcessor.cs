using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SendellVoice.Infrastructure.Documents;

/// <summary>
/// Processes documents for text extraction and chunking.
/// </summary>
public class DocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<DocumentProcessor> _logger;

    private static readonly HashSet<string> SupportedExtensionSet = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".txt", ".md", ".html", ".htm"
    };

    public IReadOnlyList<string> SupportedExtensions => SupportedExtensionSet.ToList();

    public DocumentProcessor(ILogger<DocumentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Document not found", filePath);
        }

        var extension = Path.GetExtension(filePath);

        _logger.LogInformation("Extracting text from: {FilePath} ({Extension})", filePath, extension);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => ExtractFromPdf(filePath),
            ".docx" => ExtractFromDocx(filePath),
            ".doc" => ExtractFromDoc(filePath),
            ".txt" or ".md" => await File.ReadAllTextAsync(filePath, cancellationToken),
            ".html" or ".htm" => ExtractFromHtml(await File.ReadAllTextAsync(filePath, cancellationToken)),
            _ => throw new NotSupportedException($"File type {extension} is not supported")
        };
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileType, CancellationToken cancellationToken = default)
    {
        // Ensure fileType starts with a dot
        if (!fileType.StartsWith('.'))
        {
            fileType = "." + fileType;
        }

        _logger.LogInformation("Extracting text from stream ({FileType})", fileType);

        // Copy to memory stream for processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return fileType.ToLowerInvariant() switch
        {
            ".pdf" => ExtractFromPdfStream(memoryStream),
            ".docx" => ExtractFromDocxStream(memoryStream),
            ".txt" or ".md" => await ReadStreamAsTextAsync(memoryStream, cancellationToken),
            ".html" or ".htm" => ExtractFromHtml(await ReadStreamAsTextAsync(memoryStream, cancellationToken)),
            _ => throw new NotSupportedException($"File type {fileType} is not supported")
        };
    }

    public IReadOnlyList<DocumentChunk> ChunkText(string text, string sourceFile, int chunkSize = 512, int overlap = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<DocumentChunk>();
        }

        var chunks = new List<DocumentChunk>();

        // Split by paragraphs first for better semantic boundaries
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        var currentWordCount = 0;
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphWords = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // If single paragraph exceeds chunk size, split it
            if (paragraphWords.Length > chunkSize)
            {
                // Flush current chunk first
                if (currentWordCount > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString().Trim(), sourceFile, chunkIndex++));
                    currentChunk.Clear();
                    currentWordCount = 0;
                }

                // Split large paragraph into chunks
                for (int i = 0; i < paragraphWords.Length; i += chunkSize - overlap)
                {
                    var end = Math.Min(i + chunkSize, paragraphWords.Length);
                    var chunkText = string.Join(" ", paragraphWords[i..end]);
                    chunks.Add(CreateChunk(chunkText, sourceFile, chunkIndex++));
                }
            }
            else if (currentWordCount + paragraphWords.Length > chunkSize)
            {
                // Current chunk is full, start new one
                chunks.Add(CreateChunk(currentChunk.ToString().Trim(), sourceFile, chunkIndex++));

                // Start new chunk with overlap from previous content
                currentChunk.Clear();
                currentWordCount = 0;

                currentChunk.AppendLine(paragraph);
                currentWordCount = paragraphWords.Length;
            }
            else
            {
                // Add paragraph to current chunk
                currentChunk.AppendLine(paragraph);
                currentWordCount += paragraphWords.Length;
            }
        }

        // Don't forget the last chunk
        if (currentWordCount > 0)
        {
            chunks.Add(CreateChunk(currentChunk.ToString().Trim(), sourceFile, chunkIndex));
        }

        _logger.LogInformation("Chunked document {Source} into {Count} chunks", sourceFile, chunks.Count);

        return chunks;
    }

    public bool IsSupported(string fileExtension)
    {
        if (!fileExtension.StartsWith('.'))
        {
            fileExtension = "." + fileExtension;
        }

        return SupportedExtensionSet.Contains(fileExtension);
    }

    private static DocumentChunk CreateChunk(string content, string sourceFile, int index)
    {
        return new DocumentChunk
        {
            Id = $"{Path.GetFileNameWithoutExtension(sourceFile)}_{index}_{Guid.NewGuid():N}",
            Content = content,
            SourceFile = sourceFile,
            ChunkIndex = index
        };
    }

    private string ExtractFromPdf(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return ExtractFromPdfStream(stream);
    }

    private string ExtractFromPdfStream(Stream stream)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(stream);

        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine(); // Paragraph break between pages
            }
        }

        return sb.ToString();
    }

    private string ExtractFromDocx(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return ExtractFromDocxStream(stream);
    }

    private string ExtractFromDocxStream(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);

        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var element in body.ChildElements)
        {
            var text = element.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString();
    }

    private static string ExtractFromDoc(string filePath)
    {
        // Legacy .doc format is more complex to handle
        // For now, throw a more specific exception
        throw new NotSupportedException(
            "Legacy .doc format is not supported. Please convert to .docx format.");
    }

    private static string ExtractFromHtml(string html)
    {
        // Simple HTML stripping - in production, use a proper HTML parser
        var sb = new StringBuilder();
        var inTag = false;

        foreach (var c in html)
        {
            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
                sb.Append(' ');
            }
            else if (!inTag)
            {
                sb.Append(c);
            }
        }

        // Decode common HTML entities
        var text = sb.ToString()
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"");

        // Normalize whitespace
        return string.Join(" ", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static async Task<string> ReadStreamAsTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
