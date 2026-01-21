using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Models;
using SendellVoice.Domain.Enums;

namespace SendellVoice.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin for generating contextual responses to customers.
/// </summary>
public class ResponseGenerationPlugin
{
    private readonly ILogger<ResponseGenerationPlugin> _logger;

    private const string ResponsePrompt = @"
You are a helpful customer service agent for SendellVoice, an intelligent contact center platform.
Your goal is to provide helpful, empathetic, and accurate responses to customers.

Guidelines:
- Be professional yet friendly
- Show empathy for customer issues
- Provide clear and actionable information
- If you cannot help, explain how to get further assistance
- Keep responses concise but complete
- Do not make up information - only use what's provided in the context

{{#if context}}
Relevant Information:
{{$context}}
{{/if}}

{{#if conversationHistory}}
Previous Conversation:
{{$conversationHistory}}
{{/if}}

Current Intent: {{$intent}}
Customer Message: {{$customerMessage}}

Respond to the customer in a helpful and professional manner:";

    public ResponseGenerationPlugin(ILogger<ResponseGenerationPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("generate_response")]
    [Description("Generates a contextual response to the customer's message")]
    public async Task<string> GenerateResponseAsync(
        [Description("The customer's message")] string customerMessage,
        [Description("The classified intent category")] string intent,
        [Description("Relevant context from knowledge base")] string? context,
        [Description("Previous conversation messages")] string? conversationHistory,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating response for intent: {Intent}", intent);

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are a helpful customer service agent for SendellVoice, an intelligent contact center platform.");
        promptBuilder.AppendLine("Your goal is to provide helpful, empathetic, and accurate responses to customers.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Guidelines:");
        promptBuilder.AppendLine("- Be professional yet friendly");
        promptBuilder.AppendLine("- Show empathy for customer issues");
        promptBuilder.AppendLine("- Provide clear and actionable information");
        promptBuilder.AppendLine("- If you cannot help, explain how to get further assistance");
        promptBuilder.AppendLine("- Keep responses concise but complete");
        promptBuilder.AppendLine("- Do not make up information - only use what's provided in the context");
        promptBuilder.AppendLine();

        if (!string.IsNullOrEmpty(context))
        {
            promptBuilder.AppendLine("Relevant Information:");
            promptBuilder.AppendLine(context);
            promptBuilder.AppendLine();
        }

        if (!string.IsNullOrEmpty(conversationHistory))
        {
            promptBuilder.AppendLine("Previous Conversation:");
            promptBuilder.AppendLine(conversationHistory);
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"Current Intent: {intent}");
        promptBuilder.AppendLine($"Customer Message: {customerMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Respond to the customer in a helpful and professional manner:");

        var function = kernel.CreateFunctionFromPrompt(promptBuilder.ToString());
        var result = await kernel.InvokeAsync(function, cancellationToken: cancellationToken);

        var response = result.ToString();
        _logger.LogInformation("Generated response with length: {Length}", response.Length);

        return response;
    }

    [KernelFunction("generate_response_with_context")]
    [Description("Generates a response using full conversation context")]
    public async Task<string> GenerateResponseWithContextAsync(
        [Description("The customer's message")] string customerMessage,
        ConversationContext conversationContext,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        // Build context from knowledge base results
        var contextBuilder = new StringBuilder();
        if (conversationContext.RelevantKnowledge.Any())
        {
            foreach (var knowledge in conversationContext.RelevantKnowledge)
            {
                contextBuilder.AppendLine($"Source: {knowledge.SourceFile}");
                contextBuilder.AppendLine(knowledge.Content);
                contextBuilder.AppendLine("---");
            }
        }

        // Build conversation history
        var historyBuilder = new StringBuilder();
        foreach (var message in conversationContext.RecentMessages)
        {
            var speaker = message.IsFromBot ? "Agent" : "Customer";
            historyBuilder.AppendLine($"{speaker}: {message.Content}");
        }

        var intent = conversationContext.CurrentIntent?.ToString() ?? "General";

        return await GenerateResponseAsync(
            customerMessage,
            intent,
            contextBuilder.Length > 0 ? contextBuilder.ToString() : null,
            historyBuilder.Length > 0 ? historyBuilder.ToString() : null,
            kernel,
            cancellationToken);
    }

    [KernelFunction("generate_escalation_summary")]
    [Description("Generates a summary for escalating the conversation to a human agent")]
    public async Task<string> GenerateEscalationSummaryAsync(
        [Description("The conversation context")] ConversationContext conversationContext,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating escalation summary for conversation: {ConversationId}",
            conversationContext.ConversationId);

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Generate a brief summary for escalating this conversation to a human agent.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Customer: {conversationContext.CustomerName ?? conversationContext.CustomerId}");
        promptBuilder.AppendLine($"Channel: {conversationContext.Channel}");
        promptBuilder.AppendLine($"Intent: {conversationContext.CurrentIntent}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Conversation History:");

        foreach (var message in conversationContext.RecentMessages)
        {
            var speaker = message.IsFromBot ? "Bot" : "Customer";
            promptBuilder.AppendLine($"[{message.Timestamp:HH:mm}] {speaker}: {message.Content}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Provide a summary including:");
        promptBuilder.AppendLine("1. Main issue/request");
        promptBuilder.AppendLine("2. Actions already taken");
        promptBuilder.AppendLine("3. Recommended next steps");

        var function = kernel.CreateFunctionFromPrompt(promptBuilder.ToString());
        var result = await kernel.InvokeAsync(function, cancellationToken: cancellationToken);

        return result.ToString();
    }
}
