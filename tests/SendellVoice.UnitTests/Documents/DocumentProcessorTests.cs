using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SendellVoice.Infrastructure.Documents;

namespace SendellVoice.UnitTests.Documents;

public class DocumentProcessorTests
{
    private readonly DocumentProcessor _processor;

    public DocumentProcessorTests()
    {
        var loggerMock = new Mock<ILogger<DocumentProcessor>>();
        _processor = new DocumentProcessor(loggerMock.Object);
    }

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".docx", true)]
    [InlineData(".txt", true)]
    [InlineData(".md", true)]
    [InlineData(".html", true)]
    [InlineData(".htm", true)]
    [InlineData(".xyz", false)]
    [InlineData(".exe", false)]
    [InlineData("", false)]
    public void IsSupported_ReturnsCorrectValue(string extension, bool expected)
    {
        // Act
        var result = _processor.IsSupported(extension);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SupportedExtensions_ContainsExpectedFormats()
    {
        // Act
        var extensions = _processor.SupportedExtensions;

        // Assert
        extensions.Should().Contain(".pdf");
        extensions.Should().Contain(".docx");
        extensions.Should().Contain(".txt");
        extensions.Should().Contain(".md");
        extensions.Should().Contain(".html");
    }

    [Fact]
    public void ChunkText_SplitsTextIntoChunks()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Range(1, 1000).Select(i => $"word{i}"));

        // Act
        var chunks = _processor.ChunkText(text, "test.txt", chunkSize: 100, overlap: 20);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
        {
            c.SourceFile.Should().Be("test.txt");
            c.Content.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void ChunkText_ReturnsEmptyForNullOrWhitespace()
    {
        // Act & Assert
        _processor.ChunkText(null!, "test.txt").Should().BeEmpty();
        _processor.ChunkText("", "test.txt").Should().BeEmpty();
        _processor.ChunkText("   ", "test.txt").Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_AssignsUniqueIds()
    {
        // Arrange
        var text = "This is paragraph one.\n\nThis is paragraph two.\n\nThis is paragraph three.";

        // Act
        var chunks = _processor.ChunkText(text, "test.txt");

        // Assert
        var ids = chunks.Select(c => c.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ChunkText_AssignsSequentialChunkIndices()
    {
        // Arrange
        var text = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => $"This is paragraph {i} with some content to make it longer."));

        // Act
        var chunks = _processor.ChunkText(text, "test.txt", chunkSize: 50, overlap: 10);

        // Assert
        var indices = chunks.Select(c => c.ChunkIndex).ToList();
        indices.Should().BeInAscendingOrder();
        indices[0].Should().Be(0);
    }

    [Fact]
    public async Task ExtractTextAsync_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _processor.ExtractTextAsync("/nonexistent/file.txt"));
    }

    [Fact]
    public async Task ExtractTextAsync_WithTextFile_ReturnsContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var expectedContent = "Hello, World!\nThis is a test file.";
        await File.WriteAllTextAsync(tempFile, expectedContent);

        try
        {
            // Act
            var result = await _processor.ExtractTextAsync(tempFile);

            // Assert
            result.Should().Be(expectedContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_WithMarkdownFile_ReturnsContent()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md");
        var expectedContent = "# Heading\n\nSome **bold** text.";
        await File.WriteAllTextAsync(tempFile, expectedContent);

        try
        {
            // Act
            var result = await _processor.ExtractTextAsync(tempFile);

            // Assert
            result.Should().Be(expectedContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_WithUnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xyz");
        await File.WriteAllTextAsync(tempFile, "content");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => _processor.ExtractTextAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_FromStream_WithTextContent_ReturnsText()
    {
        // Arrange
        var content = "Stream content test";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _processor.ExtractTextAsync(stream, ".txt");

        // Assert
        result.Should().Be(content);
    }
}
