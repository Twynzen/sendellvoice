using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Application.Plugins;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Enums;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Application.Services;

/// <summary>
/// Orchestrates conversation flows using Semantic Kernel and AI services.
/// </summary>
public class ConversationOrchestrator
{
    private readonly Kernel _kernel;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<ConversationOrchestrator> _logger;

    public ConversationOrchestrator(
        Kernel kernel,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<ConversationOrchestrator> logger)
    {
        _kernel = kernel;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming customer message and generates a response.
    /// </summary>
    public async Task<OrchestratorResult> ProcessMessageAsync(
        Guid conversationId,
        string customerMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message for conversation: {ConversationId}", conversationId);

        // Get or create conversation
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
            return OrchestratorResult.Failed("Conversation not found");
        }

        // Save incoming message
        var incomingMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = customerMessage,
            Direction = MessageDirection.Inbound,
            Type = MessageType.Text,
            CreatedAt = DateTime.UtcNow,
            IsFromBot = false
        };
        await _messageRepository.AddAsync(incomingMessage, cancellationToken);

        // Classify intent
        var intentPlugin = _kernel.Plugins["IntentClassification"];
        var intentResult = await _kernel.InvokeAsync<IntentResult>(
            intentPlugin["classify_intent"],
            new() { ["customerMessage"] = customerMessage },
            cancellationToken);

        // Update message with intent
        incomingMessage.DetectedIntent = intentResult.Intent;
        incomingMessage.IntentConfidence = intentResult.Confidence;
        await _messageRepository.UpdateAsync(incomingMessage, cancellationToken);

        // Update conversation with primary intent if higher confidence
        if (conversation.IntentConfidence == null || intentResult.Confidence > conversation.IntentConfidence)
        {
            conversation.PrimaryIntent = intentResult.Intent;
            conversation.IntentConfidence = intentResult.Confidence;
            await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        }

        // Check if escalation is needed
        if (ShouldEscalate(intentResult))
        {
            _logger.LogInformation("Escalation triggered for conversation: {ConversationId}", conversationId);
            return await HandleEscalationAsync(conversation, intentResult, cancellationToken);
        }

        // Search knowledge base for relevant information
        var knowledgePlugin = _kernel.Plugins["KnowledgeRetrieval"];
        var context = await _kernel.InvokeAsync<string>(
            knowledgePlugin["get_formatted_context"],
            new() { ["query"] = customerMessage, ["maxDocuments"] = 3 },
            cancellationToken);

        // Build conversation history
        var recentMessages = await _messageRepository.GetRecentMessagesAsync(conversationId, 5, cancellationToken);
        var historyBuilder = new System.Text.StringBuilder();
        foreach (var msg in recentMessages.OrderBy(m => m.CreatedAt))
        {
            var speaker = msg.IsFromBot ? "Agent" : "Customer";
            historyBuilder.AppendLine($"{speaker}: {msg.Content}");
        }

        // Generate response
        var responsePlugin = _kernel.Plugins["ResponseGeneration"];
        var response = await _kernel.InvokeAsync<string>(
            responsePlugin["generate_response"],
            new()
            {
                ["customerMessage"] = customerMessage,
                ["intent"] = intentResult.Intent.ToString(),
                ["context"] = context,
                ["conversationHistory"] = historyBuilder.ToString()
            },
            cancellationToken);

        // Save outgoing message
        var outgoingMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = response,
            Direction = MessageDirection.Outbound,
            Type = MessageType.Text,
            CreatedAt = DateTime.UtcNow,
            IsFromBot = true
        };
        await _messageRepository.AddAsync(outgoingMessage, cancellationToken);

        _logger.LogInformation("Generated response for conversation: {ConversationId}", conversationId);

        return OrchestratorResult.Success(response, intentResult);
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    public async Task<Conversation> CreateConversationAsync(
        string customerId,
        ChannelType channel,
        string? customerName = null,
        string? customerPhone = null,
        string? customerEmail = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerEmail = customerEmail,
            Channel = channel,
            Status = ConversationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation, cancellationToken);

        _logger.LogInformation("Created new conversation: {ConversationId} for customer: {CustomerId}",
            conversation.Id, customerId);

        return conversation;
    }

    /// <summary>
    /// Ends a conversation with optional summary.
    /// </summary>
    public async Task<Conversation> EndConversationAsync(
        Guid conversationId,
        ConversationStatus status = ConversationStatus.Closed,
        string? resolution = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation not found: {conversationId}");
        }

        conversation.Status = status;
        conversation.Resolution = resolution;
        conversation.EndedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        _logger.LogInformation("Ended conversation: {ConversationId} with status: {Status}",
            conversationId, status);

        return conversation;
    }

    private bool ShouldEscalate(IntentResult intentResult)
    {
        // Escalate for complaints, emergencies, or low confidence
        return intentResult.Intent == IntentCategory.Complaint ||
               intentResult.Intent == IntentCategory.Emergency ||
               intentResult.Confidence < 0.5;
    }

    private async Task<OrchestratorResult> HandleEscalationAsync(
        Conversation conversation,
        IntentResult intentResult,
        CancellationToken cancellationToken)
    {
        conversation.Status = ConversationStatus.Escalated;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        var escalationMessage = intentResult.Intent switch
        {
            IntentCategory.Emergency => "I understand this is urgent. Let me connect you with a specialist immediately.",
            IntentCategory.Complaint => "I'm sorry to hear about your experience. Let me transfer you to a supervisor who can help resolve this.",
            _ => "Let me connect you with a human agent who can better assist you with this request."
        };

        return OrchestratorResult.Escalated(escalationMessage, intentResult);
    }
}

/// <summary>
/// Result of the conversation orchestration process.
/// </summary>
public record OrchestratorResult
{
    public bool IsSuccess { get; init; }
    public bool IsEscalated { get; init; }
    public string? Response { get; init; }
    public IntentResult? Intent { get; init; }
    public string? ErrorMessage { get; init; }

    public static OrchestratorResult Success(string response, IntentResult intent) =>
        new() { IsSuccess = true, Response = response, Intent = intent };

    public static OrchestratorResult Escalated(string response, IntentResult intent) =>
        new() { IsSuccess = true, IsEscalated = true, Response = response, Intent = intent };

    public static OrchestratorResult Failed(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
