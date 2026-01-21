using System.Runtime.CompilerServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.Speech;

/// <summary>
/// Azure Speech Services-based implementation of speech-to-text service.
/// </summary>
public class AzureSpeechService : ISpeechToTextService, IDisposable
{
    private readonly AzureSpeechSettings _settings;
    private readonly ILogger<AzureSpeechService> _logger;
    private SpeechConfig? _speechConfig;

    public AzureSpeechService(IOptions<AzureSpeechSettings> options, ILogger<AzureSpeechService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        InitializeSpeechConfig();
    }

    private void InitializeSpeechConfig()
    {
        if (string.IsNullOrEmpty(_settings.SubscriptionKey) || string.IsNullOrEmpty(_settings.Region))
        {
            _logger.LogWarning("Azure Speech service not configured - SubscriptionKey or Region missing");
            return;
        }

        _speechConfig = SpeechConfig.FromSubscription(_settings.SubscriptionKey, _settings.Region);
        _speechConfig.SpeechRecognitionLanguage = _settings.Language ?? "en-US";
        _speechConfig.SetProfanity(ProfanityOption.Raw);

        _logger.LogInformation("Azure Speech service configured for region: {Region}", _settings.Region);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (_speechConfig == null)
        {
            throw new InvalidOperationException("Azure Speech service is not configured");
        }

        _logger.LogInformation("Transcribing audio with Azure Speech: {FileName}", fileName);

        var segments = new List<TranscriptionSegment>();
        var fullText = new System.Text.StringBuilder();
        TimeSpan totalDuration = TimeSpan.Zero;

        // Copy stream to memory for Azure SDK
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var pushStream = AudioInputStream.CreatePushStream();
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        var tcs = new TaskCompletionSource<bool>();

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                fullText.AppendLine(e.Result.Text);
                segments.Add(new TranscriptionSegment(
                    TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                    e.Result.Text));

                totalDuration = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks);
            }
        };

        recognizer.SessionStopped += (s, e) => tcs.TrySetResult(true);
        recognizer.Canceled += (s, e) =>
        {
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError("Azure Speech recognition error: {Error}", e.ErrorDetails);
                tcs.TrySetException(new Exception(e.ErrorDetails));
            }
            else
            {
                tcs.TrySetResult(true);
            }
        };

        await recognizer.StartContinuousRecognitionAsync();

        // Push audio data
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await memoryStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            pushStream.Write(buffer, bytesRead);
        }
        pushStream.Close();

        // Wait for recognition to complete with timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, timeoutCts.Token));
        }
        finally
        {
            await recognizer.StopContinuousRecognitionAsync();
        }

        var text = fullText.ToString().Trim();

        _logger.LogInformation("Azure Speech transcription complete: {Length} characters, {SegmentCount} segments",
            text.Length, segments.Count);

        return new TranscriptionResult(
            text,
            totalDuration,
            segments,
            _speechConfig.SpeechRecognitionLanguage);
    }

    public async IAsyncEnumerable<string> TranscribeStreamingAsync(
        Stream audioStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_speechConfig == null)
        {
            throw new InvalidOperationException("Azure Speech service is not configured");
        }

        using var pushStream = AudioInputStream.CreatePushStream();
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        var textQueue = new System.Collections.Concurrent.BlockingCollection<string>();
        var completedTcs = new TaskCompletionSource<bool>();

        recognizer.Recognizing += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                // Interim results - could be used for real-time feedback
            }
        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
            {
                textQueue.Add(e.Result.Text);
            }
        };

        recognizer.SessionStopped += (s, e) =>
        {
            textQueue.CompleteAdding();
            completedTcs.TrySetResult(true);
        };

        recognizer.Canceled += (s, e) =>
        {
            textQueue.CompleteAdding();
            if (e.Reason == CancellationReason.Error)
            {
                completedTcs.TrySetException(new Exception(e.ErrorDetails));
            }
            else
            {
                completedTcs.TrySetResult(true);
            }
        };

        await recognizer.StartContinuousRecognitionAsync();

        // Start pushing audio in background
        _ = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await audioStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                pushStream.Write(buffer, bytesRead);
            }
            pushStream.Close();
        }, cancellationToken);

        // Yield recognized text as it becomes available
        foreach (var text in textQueue.GetConsumingEnumerable(cancellationToken))
        {
            yield return text;
        }

        await recognizer.StopContinuousRecognitionAsync();
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_speechConfig != null);
    }

    public void Dispose()
    {
        // SpeechConfig doesn't require explicit disposal
        GC.SuppressFinalize(this);
    }
}
