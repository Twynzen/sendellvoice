using SendellVoice.Domain.Enums;

namespace SendellVoice.Application.Common.Models;

/// <summary>
/// Result of an intent classification operation.
/// </summary>
public record IntentResult
{
    public IntentCategory Intent { get; init; }
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
    public IReadOnlyList<string> ExtractedEntities { get; init; } = new List<string>();
    public string? SuggestedAction { get; init; }
}

/// <summary>
/// Raw JSON response from LLM for intent classification.
/// </summary>
public record IntentClassificationResponse
{
    public string Intent { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
    public List<string>? Entities { get; init; }
    public string? Action { get; init; }
}
