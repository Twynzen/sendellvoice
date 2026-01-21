using Microsoft.AspNetCore.Mvc;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Services;

namespace SendellVoice.Web.Controllers;

/// <summary>
/// API controller for speech-to-text and speech-to-action operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SpeechController : ControllerBase
{
    private readonly SpeechToActionService _speechToActionService;
    private readonly ISpeechToTextService _speechService;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(
        SpeechToActionService speechToActionService,
        ISpeechToTextService speechService,
        ILogger<SpeechController> logger)
    {
        _speechToActionService = speechToActionService;
        _speechService = speechService;
        _logger = logger;
    }

    /// <summary>
    /// Transcribes an audio file to text.
    /// </summary>
    [HttpPost("transcribe")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TranscriptionResponse>> Transcribe(
        IFormFile audioFile,
        CancellationToken cancellationToken)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { error = "No audio file provided" });
        }

        _logger.LogInformation("Transcribing audio file: {FileName} ({Size} bytes)",
            audioFile.FileName, audioFile.Length);

        try
        {
            using var stream = audioFile.OpenReadStream();
            var result = await _speechService.TranscribeAsync(stream, audioFile.FileName, cancellationToken);

            return Ok(new TranscriptionResponse
            {
                Text = result.Text,
                Duration = result.Duration.TotalSeconds,
                Language = result.Language,
                Confidence = result.Confidence,
                Segments = result.Segments.Select(s => new SegmentResponse
                {
                    Start = s.Start.TotalSeconds,
                    End = s.End.TotalSeconds,
                    Text = s.Text,
                    Confidence = s.Confidence
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio file: {FileName}", audioFile.FileName);
            return StatusCode(500, new { error = "Failed to transcribe audio", details = ex.Message });
        }
    }

    /// <summary>
    /// Processes an audio file and determines the appropriate action.
    /// </summary>
    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ActionResponse>> ProcessAudio(
        IFormFile audioFile,
        CancellationToken cancellationToken)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { error = "No audio file provided" });
        }

        _logger.LogInformation("Processing audio file: {FileName} ({Size} bytes)",
            audioFile.FileName, audioFile.Length);

        try
        {
            using var stream = audioFile.OpenReadStream();
            var result = await _speechToActionService.ProcessAudioAsync(stream, audioFile.FileName, cancellationToken);

            return Ok(new ActionResponse
            {
                ActionType = result.ActionType,
                ActionDescription = result.ActionDescription,
                Transcription = result.Transcription,
                Intent = result.Intent?.Intent.ToString(),
                IntentConfidence = result.Intent?.Confidence,
                Reasoning = result.Intent?.Reasoning,
                SuggestedAction = result.Intent?.SuggestedAction,
                RequiresHumanReview = result.RequiresHumanReview,
                Parameters = result.Parameters,
                ErrorMessage = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio file: {FileName}", audioFile.FileName);
            return StatusCode(500, new { error = "Failed to process audio", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if the speech service is available.
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<SpeechHealthResponse>> CheckHealth(CancellationToken cancellationToken)
    {
        var isAvailable = await _speechService.IsAvailableAsync(cancellationToken);

        return Ok(new SpeechHealthResponse
        {
            IsAvailable = isAvailable,
            ServiceType = _speechService.GetType().Name
        });
    }
}

#region Request/Response Models

public record TranscriptionResponse
{
    public string Text { get; init; } = string.Empty;
    public double Duration { get; init; }
    public string? Language { get; init; }
    public double? Confidence { get; init; }
    public List<SegmentResponse> Segments { get; init; } = new();
}

public record SegmentResponse
{
    public double Start { get; init; }
    public double End { get; init; }
    public string Text { get; init; } = string.Empty;
    public double? Confidence { get; init; }
}

public record ActionResponse
{
    public string ActionType { get; init; } = string.Empty;
    public string ActionDescription { get; init; } = string.Empty;
    public string? Transcription { get; init; }
    public string? Intent { get; init; }
    public double? IntentConfidence { get; init; }
    public string? Reasoning { get; init; }
    public string? SuggestedAction { get; init; }
    public bool RequiresHumanReview { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public record SpeechHealthResponse
{
    public bool IsAvailable { get; init; }
    public string ServiceType { get; init; } = string.Empty;
}

#endregion
