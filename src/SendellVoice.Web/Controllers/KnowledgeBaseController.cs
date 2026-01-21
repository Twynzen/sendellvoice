using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SendellVoice.Application.Services;
using SendellVoice.Web.Configuration;

namespace SendellVoice.Web.Controllers;

/// <summary>
/// Controlador API para gestionar la base de conocimiento RAG.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly RAGService _ragService;
    private readonly ILogger<KnowledgeBaseController> _logger;
    private readonly FileUploadSettings _uploadSettings;

    // Constantes de validación
    private const int MaxQueryLength = 4000;
    private const int MinQueryLength = 3;
    private const int MaxTopK = 20;
    private const int MinTopK = 1;
    private const int MaxCategoryLength = 100;
    private const int MaxTagLength = 50;
    private const int MaxTagsCount = 20;

    public KnowledgeBaseController(
        RAGService ragService,
        ILogger<KnowledgeBaseController> logger,
        IOptions<FileUploadSettings> uploadSettings)
    {
        _ragService = ragService;
        _logger = logger;
        _uploadSettings = uploadSettings.Value;
    }

    /// <summary>
    /// Carga e ingesta un documento en la base de conocimiento.
    /// </summary>
    /// <param name="file">Archivo a ingestar (PDF, DOCX, TXT, MD, HTML)</param>
    /// <param name="category">Categoría opcional para clasificar el documento</param>
    /// <param name="tags">Tags separados por coma (máximo 20)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(104857600)] // 100MB máximo
    public async Task<ActionResult<IngestResponse>> IngestDocument(
        IFormFile file,
        [FromForm] string? category,
        [FromForm] string? tags,
        CancellationToken cancellationToken)
    {
        // Validación de archivo
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse("No se proporcionó ningún archivo"));
        }

        // Validación de tamaño
        if (file.Length > _uploadSettings.MaxFileSizeBytes)
        {
            var maxSizeMB = _uploadSettings.MaxFileSizeBytes / (1024 * 1024);
            return StatusCode(413, new ErrorResponse($"El archivo excede el tamaño máximo permitido de {maxSizeMB}MB"));
        }

        // Validación de extensión
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_uploadSettings.AllowedExtensions.Contains(extension))
        {
            return BadRequest(new ErrorResponse(
                $"Tipo de archivo no permitido. Extensiones válidas: {string.Join(", ", _uploadSettings.AllowedExtensions)}"));
        }

        // Validación de nombre de archivo (prevenir path traversal)
        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || Path.IsPathRooted(fileName))
        {
            return BadRequest(new ErrorResponse("Nombre de archivo inválido"));
        }

        // Validación de categoría
        if (!string.IsNullOrEmpty(category))
        {
            category = category.Trim();
            if (category.Length > MaxCategoryLength)
            {
                return BadRequest(new ErrorResponse($"La categoría no puede exceder {MaxCategoryLength} caracteres"));
            }
            // Sanitizar categoría - solo alfanuméricos, espacios y guiones
            if (!IsValidCategoryOrTag(category))
            {
                return BadRequest(new ErrorResponse("La categoría contiene caracteres no permitidos"));
            }
        }

        // Validación y sanitización de tags
        IEnumerable<string>? tagList = null;
        if (!string.IsNullOrEmpty(tags))
        {
            var parsedTags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            if (parsedTags.Count > MaxTagsCount)
            {
                return BadRequest(new ErrorResponse($"No se permiten más de {MaxTagsCount} tags"));
            }

            foreach (var tag in parsedTags)
            {
                if (tag.Length > MaxTagLength)
                {
                    return BadRequest(new ErrorResponse($"Cada tag no puede exceder {MaxTagLength} caracteres"));
                }
                if (!IsValidCategoryOrTag(tag))
                {
                    return BadRequest(new ErrorResponse($"El tag '{tag}' contiene caracteres no permitidos"));
                }
            }

            tagList = parsedTags;
        }

        _logger.LogInformation("Ingesting document: {FileName} ({Size} bytes, extension: {Extension})",
            fileName, file.Length, extension);

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _ragService.IngestDocumentAsync(
                stream,
                fileName,
                extension,
                category,
                tagList,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse(result.ErrorMessage ?? "Error al procesar el documento"));
            }

            return Ok(new IngestResponse
            {
                Success = true,
                FileName = fileName,
                ChunkCount = result.ChunkCount,
                Message = $"Documento ingestado exitosamente en {result.ChunkCount} fragmentos"
            });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning("Unsupported file type: {FileName}", fileName);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting document: {FileName}", fileName);
            return StatusCode(500, new ErrorResponse("Error interno al procesar el documento"));
        }
    }

    /// <summary>
    /// Consulta la base de conocimiento con una pregunta en lenguaje natural.
    /// </summary>
    /// <param name="request">Consulta y parámetros de búsqueda</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QueryResponse>> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        // Validación de query
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ErrorResponse("La consulta no puede estar vacía"));
        }

        var query = request.Query.Trim();

        if (query.Length < MinQueryLength)
        {
            return BadRequest(new ErrorResponse($"La consulta debe tener al menos {MinQueryLength} caracteres"));
        }

        if (query.Length > MaxQueryLength)
        {
            return BadRequest(new ErrorResponse($"La consulta no puede exceder {MaxQueryLength} caracteres"));
        }

        // Validación de TopK
        var topK = Math.Clamp(request.TopK, MinTopK, MaxTopK);

        _logger.LogInformation("Processing RAG query: {QueryPreview} (TopK: {TopK})",
            query.Length > 50 ? query[..50] + "..." : query, topK);

        try
        {
            var result = await _ragService.QueryAsync(query, topK, cancellationToken);

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
            return StatusCode(500, new ErrorResponse("Error interno al procesar la consulta"));
        }
    }

    /// <summary>
    /// Obtiene todos los documentos en la base de conocimiento.
    /// </summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(IEnumerable<DocumentResponse>), StatusCodes.Status200OK)]
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
    /// Elimina un documento de la base de conocimiento.
    /// </summary>
    /// <param name="id">ID del documento a eliminar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [HttpDelete("documents/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ragService.DeleteDocumentAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ErrorResponse("Documento no encontrado"));
        }

        _logger.LogInformation("Document deleted: {DocumentId}", id);
        return NoContent();
    }

    /// <summary>
    /// Valida que una categoría o tag contenga solo caracteres permitidos.
    /// </summary>
    private static bool IsValidCategoryOrTag(string value)
    {
        return value.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
    }
}

#region Request/Response Models

/// <summary>
/// Solicitud de consulta a la base de conocimiento.
/// </summary>
public record QueryRequest
{
    /// <summary>
    /// Pregunta en lenguaje natural.
    /// </summary>
    [Required(ErrorMessage = "La consulta es requerida")]
    [StringLength(4000, MinimumLength = 3, ErrorMessage = "La consulta debe tener entre 3 y 4000 caracteres")]
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Número de documentos relevantes a recuperar (1-20).
    /// </summary>
    [Range(1, 20, ErrorMessage = "TopK debe estar entre 1 y 20")]
    public int TopK { get; init; } = 3;
}

/// <summary>
/// Respuesta de consulta RAG.
/// </summary>
public record QueryResponse
{
    public string Answer { get; init; } = string.Empty;
    public bool HasRelevantContent { get; init; }
    public List<SourceResponse> Sources { get; init; } = new();
}

/// <summary>
/// Información de fuente para respuesta RAG.
/// </summary>
public record SourceResponse
{
    public string Content { get; init; } = string.Empty;
    public string? SourceFile { get; init; }
    public double Score { get; init; }
    public int? ChunkIndex { get; init; }
}

/// <summary>
/// Respuesta de ingesta de documento.
/// </summary>
public record IngestResponse
{
    public bool Success { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int ChunkCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Información de documento.
/// </summary>
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

/// <summary>
/// Respuesta de error estándar.
/// </summary>
public record ErrorResponse(string Error, string? Details = null);

#endregion
