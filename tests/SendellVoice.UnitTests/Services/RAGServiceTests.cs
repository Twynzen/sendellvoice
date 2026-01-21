using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Application.Services;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.UnitTests.Services;

public class RAGServiceTests
{
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IVectorStoreService> _vectorStoreServiceMock;
    private readonly Mock<IDocumentProcessor> _documentProcessorMock;
    private readonly Mock<IKnowledgeRepository> _knowledgeRepositoryMock;
    private readonly Mock<ILogger<RAGService>> _loggerMock;

    public RAGServiceTests()
    {
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _vectorStoreServiceMock = new Mock<IVectorStoreService>();
        _documentProcessorMock = new Mock<IDocumentProcessor>();
        _knowledgeRepositoryMock = new Mock<IKnowledgeRepository>();
        _loggerMock = new Mock<ILogger<RAGService>>();
    }

    [Fact]
    public async Task IngestDocumentAsync_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = CreateService(kernel);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.IngestDocumentAsync("/nonexistent/file.pdf"));
    }

    [Fact]
    public async Task IngestDocumentAsync_WhenUnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = CreateService(kernel);

        _documentProcessorMock.Setup(x => x.IsSupported(".xyz")).Returns(false);

        // Create a temp file with unsupported extension
        var tempFile = Path.GetTempFileName();
        var unsupportedFile = Path.ChangeExtension(tempFile, ".xyz");
        File.Move(tempFile, unsupportedFile);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => service.IngestDocumentAsync(unsupportedFile));
        }
        finally
        {
            File.Delete(unsupportedFile);
        }
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsActiveDocuments()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = CreateService(kernel);

        var documents = new List<KnowledgeDocument>
        {
            new() { Id = Guid.NewGuid(), Title = "Doc1", FileName = "doc1.pdf", IsActive = true },
            new() { Id = Guid.NewGuid(), Title = "Doc2", FileName = "doc2.pdf", IsActive = true }
        };

        _knowledgeRepositoryMock
            .Setup(x => x.GetActiveDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await service.GetDocumentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Title == "Doc1");
        result.Should().Contain(d => d.Title == "Doc2");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentExists_ReturnsTrue()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = CreateService(kernel);

        var documentId = Guid.NewGuid();
        var document = new KnowledgeDocument
        {
            Id = documentId,
            FileName = "test.pdf"
        };

        _knowledgeRepositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _vectorStoreServiceMock
            .Setup(x => x.DeleteBySourceAsync(document.FileName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _knowledgeRepositoryMock
            .Setup(x => x.DeleteAsync(document, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteDocumentAsync(documentId);

        // Assert
        result.Should().BeTrue();
        _vectorStoreServiceMock.Verify(x => x.DeleteBySourceAsync(document.FileName, It.IsAny<CancellationToken>()), Times.Once);
        _knowledgeRepositoryMock.Verify(x => x.DeleteAsync(document, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentNotFound_ReturnsFalse()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = CreateService(kernel);

        var documentId = Guid.NewGuid();

        _knowledgeRepositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeDocument?)null);

        // Act
        var result = await service.DeleteDocumentAsync(documentId);

        // Assert
        result.Should().BeFalse();
    }

    private RAGService CreateService(Kernel kernel)
    {
        return new RAGService(
            _embeddingServiceMock.Object,
            _vectorStoreServiceMock.Object,
            _documentProcessorMock.Object,
            _knowledgeRepositoryMock.Object,
            kernel,
            _loggerMock.Object);
    }
}
