using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Application.Services;

/// <summary>
/// Service for Retrieval-Augmented Generation (RAG) operations.
/// </summary>
public class RAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly Kernel _kernel;
    private readonly ILogger<RAGService> _logger;

    public RAGService(
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        IDocumentProcessor documentProcessor,
        IKnowledgeRepository knowledgeRepository,
        Kernel kernel,
        ILogger<RAGService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _documentProcessor = documentProcessor;
        _knowledgeRepository = knowledgeRepository;
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a document into the knowledge base.
    /// </summary>
    public async Task<IngestResult> IngestDocumentAsync(
        string filePath,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ingesting document: {FilePath}", filePath);

        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Document not found", filePath);
        }

        if (!_documentProcessor.IsSupported(fileInfo.Extension))
        {
            throw new NotSupportedException($"File type {fileInfo.Extension} is not supported");
        }

        try
        {
            // Extract text from document
            var text = await _documentProcessor.ExtractTextAsync(filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from document: {FilePath}", filePath);
                return new IngestResult(false, 0, "No text could be extracted from the document");
            }

            // Chunk the document
            var chunks = _documentProcessor.ChunkText(text, fileName);

            _logger.LogInformation("Document chunked into {ChunkCount} pieces", chunks.Count);

            // Generate embeddings for all chunks
            var embeddings = await _embeddingService.GenerateBatchAsync(
                chunks.Select(c => c.Content).ToList(),
                cancellationToken);

            // Assign embeddings to chunks
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Embedding = embeddings[i];
                if (!string.IsNullOrEmpty(category))
                {
                    chunks[i].Metadata["category"] = category;
                }
            }

            // Store in vector database
            await _vectorStoreService.AddDocumentsAsync(chunks, cancellationToken);

            // Save document metadata
            var document = new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = Path.GetFileNameWithoutExtension(filePath),
                FileName = fileName,
                FilePath = filePath,
                FileType = fileInfo.Extension,
                FileSize = fileInfo.Length,
                Category = category,
                Tags = tags?.ToList() ?? new List<string>(),
                ChunkCount = chunks.Count,
                IsProcessed = true,
                ProcessedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _knowledgeRepository.AddAsync(document, cancellationToken);

            _logger.LogInformation("Successfully ingested document: {FileName} with {ChunkCount} chunks",
                fileName, chunks.Count);

            return new IngestResult(true, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest document: {FilePath}", filePath);
            return new IngestResult(false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Ingests a document from a stream.
    /// </summary>
    public async Task<IngestResult> IngestDocumentAsync(
        Stream stream,
        string fileName,
        string fileType,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ingesting document from stream: {FileName}", fileName);

        if (!_documentProcessor.IsSupported(fileType))
        {
            throw new NotSupportedException($"File type {fileType} is not supported");
        }

        try
        {
            // Extract text from stream
            var text = await _documentProcessor.ExtractTextAsync(stream, fileType, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                return new IngestResult(false, 0, "No text could be extracted from the document");
            }

            // Chunk the document
            var chunks = _documentProcessor.ChunkText(text, fileName);

            // Generate embeddings
            var embeddings = await _embeddingService.GenerateBatchAsync(
                chunks.Select(c => c.Content).ToList(),
                cancellationToken);

            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Embedding = embeddings[i];
                if (!string.IsNullOrEmpty(category))
                {
                    chunks[i].Metadata["category"] = category;
                }
            }

            // Store in vector database
            await _vectorStoreService.AddDocumentsAsync(chunks, cancellationToken);

            // Save document metadata
            var document = new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                FilePath = $"stream://{fileName}",
                FileType = fileType,
                FileSize = stream.Length,
                Category = category,
                Tags = tags?.ToList() ?? new List<string>(),
                ChunkCount = chunks.Count,
                IsProcessed = true,
                ProcessedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _knowledgeRepository.AddAsync(document, cancellationToken);

            return new IngestResult(true, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest document from stream: {FileName}", fileName);
            return new IngestResult(false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Queries the knowledge base and generates a response.
    /// </summary>
    public async Task<RAGResponse> QueryAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing RAG query: {QueryPreview}",
            query.Length > 50 ? query[..50] + "..." : query);

        // Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateAsync(query, cancellationToken);

        // Search vector store
        var searchResults = await _vectorStoreService.SearchAsync(queryEmbedding, topK, cancellationToken);

        if (!searchResults.Any())
        {
            _logger.LogInformation("No relevant documents found for query");
            return new RAGResponse
            {
                Answer = "I couldn't find any relevant information in the knowledge base to answer your question.",
                Sources = new List<SearchResult>(),
                HasRelevantContent = false
            };
        }

        // Build context from search results
        var contextBuilder = new System.Text.StringBuilder();
        foreach (var searchResult in searchResults)
        {
            contextBuilder.AppendLine($"Source: {searchResult.SourceFile}");
            contextBuilder.AppendLine(searchResult.Content);
            contextBuilder.AppendLine("---");
        }

        // Generate response using RAG prompt
        var prompt = $@"
You are a helpful assistant for SendellVoice contact center platform.
Answer the question based ONLY on the following context.
If the answer is not in the context, say so clearly.
Do not make up information.

Context:
{contextBuilder}

Question: {query}

Answer:";

        var function = _kernel.CreateFunctionFromPrompt(prompt);
        var result = await _kernel.InvokeAsync(function, cancellationToken: cancellationToken);

        var answer = result.ToString();

        _logger.LogInformation("Generated RAG response with {SourceCount} sources", searchResults.Count);

        return new RAGResponse
        {
            Answer = answer,
            Sources = searchResults.ToList(),
            HasRelevantContent = true
        };
    }

    /// <summary>
    /// Deletes a document from the knowledge base.
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _knowledgeRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return false;
        }

        // Delete from vector store
        await _vectorStoreService.DeleteBySourceAsync(document.FileName, cancellationToken);

        // Delete from repository
        await _knowledgeRepository.DeleteAsync(document, cancellationToken);

        _logger.LogInformation("Deleted document: {DocumentId} ({FileName})", documentId, document.FileName);

        return true;
    }

    /// <summary>
    /// Gets all active documents in the knowledge base.
    /// </summary>
    public async Task<IReadOnlyList<KnowledgeDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _knowledgeRepository.GetActiveDocumentsAsync(cancellationToken);
    }
}

/// <summary>
/// Result of a document ingestion operation.
/// </summary>
public record IngestResult(bool Success, int ChunkCount, string? ErrorMessage = null);

/// <summary>
/// Response from a RAG query.
/// </summary>
public record RAGResponse
{
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<SearchResult> Sources { get; init; } = new List<SearchResult>();
    public bool HasRelevantContent { get; init; }
}
