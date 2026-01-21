using Microsoft.AspNetCore.Mvc;
using SendellVoice.Application.Services;
using SendellVoice.Domain.Entities;

namespace SendellVoice.Web.Controllers;

/// <summary>
/// API controller for managing the RAG knowledge base.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly RAGService _ragService;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(RAGService ragService, ILogger<KnowledgeBaseController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads and ingests a document into the knowledge base.
    /// </summary>
    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<IngestResponse>> IngestDocument(
        IFormFile file,
        [FromForm] string? category,
        [FromForm] string? tags,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        _logger.LogInformation("Ingesting document: {FileName} ({Size} bytes)",
            file.FileName, file.Length);

        try
        {
            var tagList = string.IsNullOrEmpty(tags)
                ? null
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());

            using var stream = file.OpenReadStream();
            var result = await _ragService.IngestDocumentAsync(
                stream,
                file.FileName,
                Path.GetExtension(file.FileName),
                category,
                tagList,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new IngestResponse
            {
                Success = true,
                FileName = file.FileName,
                ChunkCount = result.ChunkCount,
                Message = $"Successfully ingested document into {result.ChunkCount} chunks"
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting document: {FileName}", file.FileName);
            return StatusCode(500, new { error = "Failed to ingest document", details = ex.Message });
        }
    }

    /// <summary>
    /// Queries the knowledge base with a natural language question.
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<QueryResponse>> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query cannot be empty" });
        }

        _logger.LogInformation("Processing RAG query: {QueryPreview}",
            request.Query.Length > 50 ? request.Query[..50] + "..." : request.Query);

        try
        {
            var result = await _ragService.QueryAsync(request.Query, request.TopK, cancellationToken);

            return Ok(new QueryResponse
            {
                Answer = result.Answer,
                HasRelevantContent = result.HasRelevantContent,
                Sources = result.Sources.Select(s => new SourceResponse
                {
                    Content = s.Content,
                    SourceFile = s.SourceFile,
                    Score = s.Score,
                    ChunkIndex = s.ChunkIndex
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return StatusCode(500, new { error = "Failed to process query", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets all documents in the knowledge base.
    /// </summary>
    [HttpGet("documents")]
    public async Task<ActionResult<IEnumerable<DocumentResponse>>> GetDocuments(CancellationToken cancellationToken)
    {
        var documents = await _ragService.GetDocumentsAsync(cancellationToken);

        return Ok(documents.Select(d => new DocumentResponse
        {
            Id = d.Id,
            Title = d.Title,
            FileName = d.FileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            Category = d.Category,
            Tags = d.Tags,
            ChunkCount = d.ChunkCount,
            IsProcessed = d.IsProcessed,
            ProcessedAt = d.ProcessedAt,
            CreatedAt = d.CreatedAt
        }));
    }

    /// <summary>
    /// Deletes a document from the knowledge base.
    /// </summary>
    [HttpDelete("documents/{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ragService.DeleteDocumentAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { error = "Document not found" });
        }

        return NoContent();
    }
}

#region Request/Response Models

public record QueryRequest
{
    public string Query { get; init; } = string.Empty;
    public int TopK { get; init; } = 3;
}

public record QueryResponse
{
    public string Answer { get; init; } = string.Empty;
    public bool HasRelevantContent { get; init; }
    public List<SourceResponse> Sources { get; init; } = new();
}

public record SourceResponse
{
    public string Content { get; init; } = string.Empty;
    public string? SourceFile { get; init; }
    public double Score { get; init; }
    public int? ChunkIndex { get; init; }
}

public record IngestResponse
{
    public bool Success { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int ChunkCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record DocumentResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? Category { get; init; }
    public List<string> Tags { get; init; } = new();
    public int ChunkCount { get; init; }
    public bool IsProcessed { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
