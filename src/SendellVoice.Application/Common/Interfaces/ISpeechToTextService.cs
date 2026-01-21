using SendellVoice.Application.Common.Models;

namespace SendellVoice.Application.Common.Interfaces;

/// <summary>
/// Interface for speech-to-text transcription services.
/// </summary>
public interface ISpeechToTextService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> TranscribeStreamingAsync(Stream audioStream, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
