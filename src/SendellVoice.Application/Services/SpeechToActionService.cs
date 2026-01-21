using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Domain.Enums;

namespace SendellVoice.Application.Services;

/// <summary>
/// Service for processing audio input and converting it to actionable results.
/// </summary>
public class SpeechToActionService
{
    private readonly ISpeechToTextService _speechService;
    private readonly Kernel _kernel;
    private readonly ILogger<SpeechToActionService> _logger;

    public SpeechToActionService(
        ISpeechToTextService speechService,
        Kernel kernel,
        ILogger<SpeechToActionService> logger)
    {
        _speechService = speechService;
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Processes an audio stream and returns an actionable result.
    /// </summary>
    public async Task<ActionResult> ProcessAudioAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing audio file: {FileName}", fileName);

        try
        {
            // Step 1: Transcribe the audio
            var transcription = await _speechService.TranscribeAsync(audioStream, fileName, cancellationToken);

            if (string.IsNullOrWhiteSpace(transcription.Text))
            {
                _logger.LogWarning("Transcription resulted in empty text");
                return new ActionResult
                {
                    ActionType = "no_speech_detected",
                    ActionDescription = "No speech was detected in the audio",
                    Transcription = string.Empty,
                    RequiresHumanReview = true
                };
            }

            _logger.LogInformation("Transcribed {Duration:F1}s of audio", transcription.Duration.TotalSeconds);

            // Step 2: Classify the intent
            var intentPlugin = _kernel.Plugins["IntentClassification"];
            var intentResult = await _kernel.InvokeAsync<IntentResult>(
                intentPlugin["classify_intent"],
                new() { ["customerMessage"] = transcription.Text },
                cancellationToken);

            // Step 3: Determine action based on intent
            var action = DetermineAction(intentResult);

            _logger.LogInformation("Determined action: {ActionType} for intent: {Intent}",
                action.ActionType, intentResult.Intent);

            return action with
            {
                Transcription = transcription.Text,
                Intent = intentResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio: {FileName}", fileName);
            return new ActionResult
            {
                ActionType = "error",
                ActionDescription = "Failed to process audio",
                ErrorMessage = ex.Message,
                RequiresHumanReview = true
            };
        }
    }

    /// <summary>
    /// Processes audio with streaming transcription for real-time feedback.
    /// </summary>
    public async IAsyncEnumerable<StreamingActionUpdate> ProcessAudioStreamingAsync(
        Stream audioStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var transcriptionBuilder = new System.Text.StringBuilder();

        await foreach (var segment in _speechService.TranscribeStreamingAsync(audioStream, cancellationToken))
        {
            transcriptionBuilder.Append(segment);

            yield return new StreamingActionUpdate
            {
                UpdateType = StreamingUpdateType.Transcription,
                Content = segment,
                FullTranscription = transcriptionBuilder.ToString()
            };
        }

        var fullTranscription = transcriptionBuilder.ToString();

        if (!string.IsNullOrWhiteSpace(fullTranscription))
        {
            // Classify intent on complete transcription
            var intentPlugin = _kernel.Plugins["IntentClassification"];
            var intentResult = await _kernel.InvokeAsync<IntentResult>(
                intentPlugin["classify_intent"],
                new() { ["customerMessage"] = fullTranscription },
                cancellationToken);

            var action = DetermineAction(intentResult);

            yield return new StreamingActionUpdate
            {
                UpdateType = StreamingUpdateType.IntentClassified,
                Intent = intentResult
            };

            yield return new StreamingActionUpdate
            {
                UpdateType = StreamingUpdateType.ActionDetermined,
                Action = action with { Transcription = fullTranscription, Intent = intentResult }
            };
        }
    }

    /// <summary>
    /// Determines the appropriate action based on the classified intent.
    /// </summary>
    private ActionResult DetermineAction(IntentResult intentResult)
    {
        var (actionType, description, requiresReview) = intentResult.Intent switch
        {
            IntentCategory.Billing => ("route_to_billing", "Route to billing department", false),
            IntentCategory.Technical => ("create_support_ticket", "Create technical support ticket", false),
            IntentCategory.Account => ("route_to_account_services", "Route to account services", false),
            IntentCategory.Complaint => ("escalate_to_supervisor", "Escalate to supervisor for review", true),
            IntentCategory.Sales => ("route_to_sales", "Route to sales team", false),
            IntentCategory.Cancellation => ("initiate_retention_flow", "Initiate customer retention flow", true),
            IntentCategory.Feedback => ("log_feedback", "Log customer feedback", false),
            IntentCategory.Emergency => ("urgent_escalation", "Urgent escalation to priority support", true),
            IntentCategory.General => ("continue_conversation", "Continue with automated conversation", false),
            _ => ("route_to_agent", "Route to available agent", true)
        };

        // Low confidence should always require human review
        if (intentResult.Confidence < 0.7)
        {
            requiresReview = true;
        }

        return new ActionResult
        {
            ActionType = actionType,
            ActionDescription = description,
            RequiresHumanReview = requiresReview,
            Parameters = new Dictionary<string, object>
            {
                ["intent"] = intentResult.Intent.ToString(),
                ["confidence"] = intentResult.Confidence,
                ["suggestedAction"] = intentResult.SuggestedAction ?? string.Empty
            }
        };
    }
}

/// <summary>
/// Types of streaming updates during audio processing.
/// </summary>
public enum StreamingUpdateType
{
    Transcription,
    IntentClassified,
    ActionDetermined
}

/// <summary>
/// Update during streaming audio processing.
/// </summary>
public record StreamingActionUpdate
{
    public StreamingUpdateType UpdateType { get; init; }
    public string? Content { get; init; }
    public string? FullTranscription { get; init; }
    public IntentResult? Intent { get; init; }
    public ActionResult? Action { get; init; }
}
