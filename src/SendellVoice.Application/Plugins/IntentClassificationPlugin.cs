using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Models;
using SendellVoice.Domain.Enums;

namespace SendellVoice.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin for classifying customer intents.
/// </summary>
public class IntentClassificationPlugin
{
    private readonly ILogger<IntentClassificationPlugin> _logger;

    private const string ClassificationPrompt = @"
You are an AI assistant specialized in customer service intent classification for a contact center.

Classify the following customer message into ONE of these categories:
- BILLING: Questions about invoices, payments, refunds, charges, pricing
- TECHNICAL: Technical issues, errors, bugs, system problems, connectivity issues
- ACCOUNT: Account management, password reset, profile updates, login issues
- COMPLAINT: Complaints, dissatisfaction, requests for escalation, negative feedback
- SALES: Interest in products, upgrades, new services, pricing inquiries
- CANCELLATION: Cancel service, terminate subscription, close account
- FEEDBACK: Positive feedback, suggestions, feature requests
- EMERGENCY: Urgent issues requiring immediate attention, security concerns
- GENERAL: Greetings, thank you messages, general inquiries, other

Customer Message: {{$customerMessage}}

Respond ONLY in JSON format with no additional text:
{
    ""intent"": ""CATEGORY"",
    ""confidence"": 0.95,
    ""reasoning"": ""Brief explanation for the classification"",
    ""entities"": [""extracted relevant entities""],
    ""action"": ""Suggested action to take""
}";

    public IntentClassificationPlugin(ILogger<IntentClassificationPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("classify_intent")]
    [Description("Classifies the customer's intent from their message into predefined categories")]
    public async Task<IntentResult> ClassifyIntentAsync(
        [Description("The customer's message to classify")] string customerMessage,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Classifying intent for message: {MessagePreview}",
            customerMessage.Length > 50 ? customerMessage[..50] + "..." : customerMessage);

        var function = kernel.CreateFunctionFromPrompt(ClassificationPrompt);
        var result = await kernel.InvokeAsync(function, new() { ["customerMessage"] = customerMessage }, cancellationToken);

        var responseText = result.ToString();

        try
        {
            var response = JsonSerializer.Deserialize<IntentClassificationResponse>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
            {
                _logger.LogWarning("Failed to deserialize intent classification response");
                return CreateDefaultResult();
            }

            var intentCategory = ParseIntentCategory(response.Intent);

            _logger.LogInformation("Classified intent: {Intent} with confidence: {Confidence}",
                intentCategory, response.Confidence);

            return new IntentResult
            {
                Intent = intentCategory,
                Confidence = response.Confidence,
                Reasoning = response.Reasoning,
                ExtractedEntities = response.Entities ?? new List<string>(),
                SuggestedAction = response.Action
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse intent classification response: {Response}", responseText);
            return CreateDefaultResult();
        }
    }

    [KernelFunction("batch_classify_intents")]
    [Description("Classifies multiple customer messages in batch")]
    public async Task<IReadOnlyList<IntentResult>> BatchClassifyIntentsAsync(
        [Description("List of customer messages to classify")] IEnumerable<string> messages,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IntentResult>();

        foreach (var message in messages)
        {
            var result = await ClassifyIntentAsync(message, kernel, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private static IntentCategory ParseIntentCategory(string intent)
    {
        return intent.ToUpperInvariant() switch
        {
            "BILLING" => IntentCategory.Billing,
            "TECHNICAL" => IntentCategory.Technical,
            "ACCOUNT" => IntentCategory.Account,
            "COMPLAINT" => IntentCategory.Complaint,
            "SALES" => IntentCategory.Sales,
            "CANCELLATION" => IntentCategory.Cancellation,
            "FEEDBACK" => IntentCategory.Feedback,
            "EMERGENCY" => IntentCategory.Emergency,
            "GENERAL" => IntentCategory.General,
            _ => IntentCategory.Unknown
        };
    }

    private static IntentResult CreateDefaultResult()
    {
        return new IntentResult
        {
            Intent = IntentCategory.Unknown,
            Confidence = 0.0,
            Reasoning = "Failed to classify intent",
            ExtractedEntities = new List<string>(),
            SuggestedAction = "route_to_agent"
        };
    }
}
