using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Infrastructure.Configuration;
using Whisper.net;
using Whisper.net.Ggml;

namespace SendellVoice.Infrastructure.Speech;

/// <summary>
/// Whisper.NET-based implementation of speech-to-text service.
/// </summary>
public class WhisperNetService : ISpeechToTextService, IDisposable
{
    private readonly WhisperSettings _settings;
    private readonly ILogger<WhisperNetService> _logger;
    private WhisperProcessor? _processor;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public WhisperNetService(IOptions<WhisperSettings> options, ILogger<WhisperNetService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            var modelPath = _settings.ModelPath;

            // Download model if it doesn't exist
            if (!File.Exists(modelPath))
            {
                _logger.LogInformation("Downloading Whisper model to: {Path}", modelPath);

                var modelDir = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
                {
                    Directory.CreateDirectory(modelDir);
                }

                var modelType = GetModelTypeFromPath(modelPath);
                using var stream = await WhisperGgmlDownloader.GetGgmlModelAsync(modelType, cancellationToken);
                await using var file = File.Create(modelPath);
                await stream.CopyToAsync(file, cancellationToken);

                _logger.LogInformation("Model downloaded successfully");
            }

            var factory = WhisperFactory.FromPath(modelPath);
            var builder = factory.CreateBuilder()
                .WithThreads(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1);

            if (!string.IsNullOrEmpty(_settings.Language) && _settings.Language != "auto")
            {
                builder.WithLanguage(_settings.Language);
            }
            else
            {
                builder.WithLanguageDetection();
            }

            _processor = builder.Build();
            _initialized = true;

            _logger.LogInformation("Whisper processor initialized with model: {Path}", modelPath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static GgmlType GetModelTypeFromPath(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        return fileName switch
        {
            var f when f.Contains("tiny") => GgmlType.Tiny,
            var f when f.Contains("small") => GgmlType.Small,
            var f when f.Contains("medium") => GgmlType.Medium,
            var f when f.Contains("large") => GgmlType.LargeV3,
            _ => GgmlType.Base // Default to base model
        };
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        _logger.LogInformation("Transcribing audio file: {FileName}", fileName);

        var segments = new List<TranscriptionSegment>();
        var fullText = new StringBuilder();
        TimeSpan lastEnd = TimeSpan.Zero;
        string? detectedLanguage = null;

        try
        {
            // Convert to WAV format if needed
            var processedStream = await PreprocessAudioAsync(audioStream, fileName, cancellationToken);

            await foreach (var result in _processor!.ProcessAsync(processedStream, cancellationToken))
            {
                fullText.AppendLine(result.Text.Trim());
                segments.Add(new TranscriptionSegment(
                    result.Start,
                    result.End,
                    result.Text.Trim(),
                    result.Probability));

                lastEnd = result.End;
                detectedLanguage ??= result.Language;
            }

            var text = fullText.ToString().Trim();

            _logger.LogInformation("Transcription complete: {Length} characters, {SegmentCount} segments",
                text.Length, segments.Count);

            return new TranscriptionResult(
                text,
                lastEnd,
                segments,
                detectedLanguage,
                segments.Any() ? segments.Average(s => s.Confidence ?? 0) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transcription of: {FileName}", fileName);
            throw;
        }
    }

    public async IAsyncEnumerable<string> TranscribeStreamingAsync(
        Stream audioStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        _logger.LogInformation("Starting streaming transcription");

        await foreach (var result in _processor!.ProcessAsync(audioStream, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                yield return result.Text.Trim();
            }
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureInitializedAsync(cancellationToken);
            return _processor != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Stream> PreprocessAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken)
    {
        // Whisper expects 16kHz mono WAV audio
        // For simplicity, we'll return the stream as-is and let Whisper handle it
        // In production, you might want to use NAudio or FFmpeg for conversion

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".wav")
        {
            // Reset stream position if needed
            if (audioStream.CanSeek)
            {
                audioStream.Position = 0;
            }
            return audioStream;
        }

        // For other formats, copy to a memory stream
        // In a real implementation, convert to proper WAV format
        var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _initLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
