namespace SendellVoice.Application.Common.Models;

/// <summary>
/// Result of a speech-to-text transcription operation.
/// </summary>
public record TranscriptionResult(
    string Text,
    TimeSpan Duration,
    IReadOnlyList<TranscriptionSegment> Segments,
    string? Language = null,
    double? Confidence = null
);

/// <summary>
/// A segment of transcribed text with timing information.
/// </summary>
public record TranscriptionSegment(
    TimeSpan Start,
    TimeSpan End,
    string Text,
    double? Confidence = null
);
