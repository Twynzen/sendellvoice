using Microsoft.AspNetCore.Mvc;
using SendellVoice.Application.Services;
using SendellVoice.Domain.Enums;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Web.Controllers;

/// <summary>
/// API controller for managing conversations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly ConversationOrchestrator _orchestrator;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        ConversationOrchestrator orchestrator,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ILogger<ConversationsController> logger)
    {
        _orchestrator = orchestrator;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConversationResponse>> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating conversation for customer: {CustomerId}", request.CustomerId);

        var conversation = await _orchestrator.CreateConversationAsync(
            request.CustomerId,
            request.Channel,
            request.CustomerName,
            request.CustomerPhone,
            request.CustomerEmail,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetConversation),
            new { id = conversation.Id },
            ConversationResponse.FromEntity(conversation));
    }

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationResponse>> GetConversation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(id, cancellationToken);

        if (conversation == null)
        {
            return NotFound();
        }

        return Ok(ConversationResponse.FromEntity(conversation));
    }

    /// <summary>
    /// Sends a message to a conversation and gets an AI-generated response.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing message for conversation: {ConversationId}", id);

        var result = await _orchestrator.ProcessMessageAsync(id, request.Content, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new MessageResponse
        {
            Response = result.Response,
            Intent = result.Intent?.Intent.ToString(),
            Confidence = result.Intent?.Confidence,
            IsEscalated = result.IsEscalated,
            SuggestedAction = result.Intent?.SuggestedAction
        });
    }

    /// <summary>
    /// Gets all messages for a conversation.
    /// </summary>
    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IEnumerable<ConversationMessageResponse>>> GetMessages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var messages = await _messageRepository.GetByConversationIdAsync(id, cancellationToken);

        return Ok(messages.Select(m => new ConversationMessageResponse
        {
            Id = m.Id,
            Content = m.Content,
            Direction = m.Direction.ToString(),
            Type = m.Type.ToString(),
            IsFromBot = m.IsFromBot,
            DetectedIntent = m.DetectedIntent?.ToString(),
            IntentConfidence = m.IntentConfidence,
            CreatedAt = m.CreatedAt
        }));
    }

    /// <summary>
    /// Gets active conversations.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetActiveConversations(
        CancellationToken cancellationToken)
    {
        var conversations = await _conversationRepository.GetActiveConversationsAsync(cancellationToken);

        return Ok(conversations.Select(ConversationResponse.FromEntity));
    }

    /// <summary>
    /// Gets conversations by customer ID.
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetByCustomer(
        string customerId,
        CancellationToken cancellationToken)
    {
        var conversations = await _conversationRepository.GetByCustomerIdAsync(customerId, cancellationToken);

        return Ok(conversations.Select(ConversationResponse.FromEntity));
    }

    /// <summary>
    /// Ends a conversation.
    /// </summary>
    [HttpPost("{id:guid}/end")]
    public async Task<ActionResult<ConversationResponse>> EndConversation(
        Guid id,
        [FromBody] EndConversationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _orchestrator.EndConversationAsync(
                id,
                request.Status,
                request.Resolution,
                cancellationToken);

            return Ok(ConversationResponse.FromEntity(conversation));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

#region Request/Response Models

public record CreateConversationRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public ChannelType Channel { get; init; } = ChannelType.Chat;
}

public record SendMessageRequest
{
    public string Content { get; init; } = string.Empty;
}

public record EndConversationRequest
{
    public ConversationStatus Status { get; init; } = ConversationStatus.Closed;
    public string? Resolution { get; init; }
}

public record ConversationResponse
{
    public Guid Id { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? PrimaryIntent { get; init; }
    public double? IntentConfidence { get; init; }
    public string? Summary { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int MessageCount { get; init; }

    public static ConversationResponse FromEntity(Domain.Entities.Conversation entity) => new()
    {
        Id = entity.Id,
        CustomerId = entity.CustomerId,
        CustomerName = entity.CustomerName,
        Channel = entity.Channel.ToString(),
        Status = entity.Status.ToString(),
        PrimaryIntent = entity.PrimaryIntent?.ToString(),
        IntentConfidence = entity.IntentConfidence,
        Summary = entity.Summary,
        CreatedAt = entity.CreatedAt,
        EndedAt = entity.EndedAt,
        MessageCount = entity.Messages?.Count ?? 0
    };
}

public record MessageResponse
{
    public string? Response { get; init; }
    public string? Intent { get; init; }
    public double? Confidence { get; init; }
    public bool IsEscalated { get; init; }
    public string? SuggestedAction { get; init; }
}

public record ConversationMessageResponse
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsFromBot { get; init; }
    public string? DetectedIntent { get; init; }
    public double? IntentConfidence { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
