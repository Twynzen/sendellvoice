namespace SendellVoice.Application.Common.Models;

/// <summary>
/// Result of a speech-to-action processing operation.
/// </summary>
public record ActionResult
{
    public string ActionType { get; init; } = string.Empty;
    public string ActionDescription { get; init; } = string.Empty;
    public string? Transcription { get; init; }
    public IntentResult? Intent { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public bool RequiresHumanReview { get; init; }
    public string? ErrorMessage { get; init; }
}
