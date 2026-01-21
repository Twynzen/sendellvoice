using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SendellVoice.Application.Common.Models;
using SendellVoice.Infrastructure.VectorStore;

namespace SendellVoice.UnitTests.VectorStore;

public class InMemoryVectorStoreTests
{
    private readonly InMemoryVectorStore _vectorStore;

    public InMemoryVectorStoreTests()
    {
        var loggerMock = new Mock<ILogger<InMemoryVectorStore>>();
        _vectorStore = new InMemoryVectorStore(loggerMock.Object);
    }

    [Fact]
    public async Task AddDocumentAsync_StoresDocument()
    {
        // Arrange
        var chunk = new DocumentChunk
        {
            Id = "test-1",
            Content = "Test content",
            SourceFile = "test.pdf",
            ChunkIndex = 0,
            Embedding = new float[] { 0.1f, 0.2f, 0.3f }
        };

        // Act
        await _vectorStore.AddDocumentAsync(chunk);

        // Assert
        var results = await _vectorStore.SearchAsync(chunk.Embedding, 1);
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("test-1");
        results[0].Content.Should().Be("Test content");
    }

    [Fact]
    public async Task AddDocumentAsync_WithoutEmbedding_ThrowsArgumentException()
    {
        // Arrange
        var chunk = new DocumentChunk
        {
            Id = "test-1",
            Content = "Test content",
            SourceFile = "test.pdf",
            ChunkIndex = 0,
            Embedding = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _vectorStore.AddDocumentAsync(chunk));
    }

    [Fact]
    public async Task AddDocumentsAsync_StoresMultipleDocuments()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = "test-1",
                Content = "First document",
                SourceFile = "test.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 1.0f, 0.0f, 0.0f }
            },
            new()
            {
                Id = "test-2",
                Content = "Second document",
                SourceFile = "test.pdf",
                ChunkIndex = 1,
                Embedding = new float[] { 0.0f, 1.0f, 0.0f }
            }
        };

        // Act
        await _vectorStore.AddDocumentsAsync(chunks);

        // Assert
        var results = await _vectorStore.SearchAsync(new float[] { 1.0f, 0.0f, 0.0f }, 10);
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedBySimilarity()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = "similar",
                Content = "Similar document",
                SourceFile = "test.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 0.9f, 0.1f, 0.0f }
            },
            new()
            {
                Id = "different",
                Content = "Different document",
                SourceFile = "test.pdf",
                ChunkIndex = 1,
                Embedding = new float[] { 0.0f, 0.0f, 1.0f }
            }
        };

        await _vectorStore.AddDocumentsAsync(chunks);

        // Act
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };
        var results = await _vectorStore.SearchAsync(queryVector, 2);

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be("similar"); // More similar to query
        results[0].Score.Should().BeGreaterThan(results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_WithTopK_LimitsResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var chunk = new DocumentChunk
            {
                Id = $"test-{i}",
                Content = $"Document {i}",
                SourceFile = "test.pdf",
                ChunkIndex = i,
                Embedding = new float[] { (float)i / 10, 0.5f, 0.5f }
            };
            await _vectorStore.AddDocumentAsync(chunk);
        }

        // Act
        var results = await _vectorStore.SearchAsync(new float[] { 1.0f, 0.5f, 0.5f }, 3);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_FiltersResults()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = "billing-1",
                Content = "Billing document",
                SourceFile = "billing.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 1.0f, 0.0f, 0.0f },
                Metadata = new Dictionary<string, string> { ["category"] = "billing" }
            },
            new()
            {
                Id = "technical-1",
                Content = "Technical document",
                SourceFile = "technical.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 1.0f, 0.0f, 0.0f },
                Metadata = new Dictionary<string, string> { ["category"] = "technical" }
            }
        };

        await _vectorStore.AddDocumentsAsync(chunks);

        // Act
        var results = await _vectorStore.SearchAsync(
            new float[] { 1.0f, 0.0f, 0.0f },
            "category:billing",
            10);

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("billing-1");
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesDocument()
    {
        // Arrange
        var chunk = new DocumentChunk
        {
            Id = "to-delete",
            Content = "Document to delete",
            SourceFile = "test.pdf",
            ChunkIndex = 0,
            Embedding = new float[] { 1.0f, 0.0f, 0.0f }
        };

        await _vectorStore.AddDocumentAsync(chunk);

        // Act
        await _vectorStore.DeleteDocumentAsync("to-delete");

        // Assert
        var results = await _vectorStore.SearchAsync(chunk.Embedding, 10);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBySourceAsync_RemovesAllDocumentsFromSource()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = "source1-1",
                Content = "Document 1",
                SourceFile = "source1.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 1.0f, 0.0f, 0.0f }
            },
            new()
            {
                Id = "source1-2",
                Content = "Document 2",
                SourceFile = "source1.pdf",
                ChunkIndex = 1,
                Embedding = new float[] { 0.0f, 1.0f, 0.0f }
            },
            new()
            {
                Id = "source2-1",
                Content = "Document 3",
                SourceFile = "source2.pdf",
                ChunkIndex = 0,
                Embedding = new float[] { 0.0f, 0.0f, 1.0f }
            }
        };

        await _vectorStore.AddDocumentsAsync(chunks);

        // Act
        await _vectorStore.DeleteBySourceAsync("source1.pdf");

        // Assert
        var results = await _vectorStore.SearchAsync(new float[] { 1.0f, 1.0f, 1.0f }, 10);
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("source2-1");
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        // Act
        var result = await _vectorStore.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }
}
