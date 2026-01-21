using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Common.Models;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Enums;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Application.Services;

/// <summary>
/// Orquesta flujos de conversación usando Semantic Kernel y servicios de IA.
/// </summary>
public class ConversationOrchestrator
{
    private readonly Kernel _kernel;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<ConversationOrchestrator> _logger;

    // Nombres de plugins para evitar strings mágicos
    private const string IntentClassificationPluginName = "IntentClassification";
    private const string KnowledgeRetrievalPluginName = "KnowledgeRetrieval";
    private const string ResponseGenerationPluginName = "ResponseGeneration";

    public ConversationOrchestrator(
        Kernel kernel,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<ConversationOrchestrator> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validar que los plugins requeridos estén registrados
        ValidateRequiredPlugins();
    }

    /// <summary>
    /// Valida que todos los plugins requeridos estén registrados en el kernel.
    /// </summary>
    private void ValidateRequiredPlugins()
    {
        var requiredPlugins = new[]
        {
            IntentClassificationPluginName,
            KnowledgeRetrievalPluginName,
            ResponseGenerationPluginName
        };

        var missingPlugins = requiredPlugins
            .Where(name => !_kernel.Plugins.TryGetPlugin(name, out _))
            .ToList();

        if (missingPlugins.Any())
        {
            var message = $"Plugins requeridos no encontrados: {string.Join(", ", missingPlugins)}";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        _logger.LogDebug("Todos los plugins requeridos están registrados");
    }

    /// <summary>
    /// Obtiene un plugin de forma segura con manejo de errores apropiado.
    /// </summary>
    private KernelPlugin GetRequiredPlugin(string pluginName)
    {
        if (_kernel.Plugins.TryGetPlugin(pluginName, out var plugin))
        {
            return plugin;
        }

        var message = $"Plugin requerido no encontrado: {pluginName}";
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Procesa un mensaje entrante del cliente y genera una respuesta.
    /// </summary>
    public async Task<OrchestratorResult> ProcessMessageAsync(
        Guid conversationId,
        string customerMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerMessage, nameof(customerMessage));

        _logger.LogInformation("Procesando mensaje para conversación: {ConversationId}", conversationId);

        try
        {
            // Obtener conversación
            var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                _logger.LogWarning("Conversación no encontrada: {ConversationId}", conversationId);
                return OrchestratorResult.Failed("Conversación no encontrada");
            }

            // Guardar mensaje entrante
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

            // Clasificar intención
            var intentResult = await ClassifyIntentAsync(customerMessage, cancellationToken);

            // Actualizar mensaje con intención
            incomingMessage.DetectedIntent = intentResult.Intent;
            incomingMessage.IntentConfidence = intentResult.Confidence;
            await _messageRepository.UpdateAsync(incomingMessage, cancellationToken);

            // Actualizar conversación si la confianza es mayor
            if (conversation.IntentConfidence == null || intentResult.Confidence > conversation.IntentConfidence)
            {
                conversation.PrimaryIntent = intentResult.Intent;
                conversation.IntentConfidence = intentResult.Confidence;
                await _conversationRepository.UpdateAsync(conversation, cancellationToken);
            }

            // Verificar si se necesita escalación
            if (ShouldEscalate(intentResult))
            {
                _logger.LogInformation("Escalación activada para conversación: {ConversationId}", conversationId);
                return await HandleEscalationAsync(conversation, intentResult, cancellationToken);
            }

            // Buscar información relevante en base de conocimiento
            var context = await GetKnowledgeContextAsync(customerMessage, cancellationToken);

            // Construir historial de conversación
            var conversationHistory = await BuildConversationHistoryAsync(conversationId, cancellationToken);

            // Generar respuesta
            var response = await GenerateResponseAsync(
                customerMessage,
                intentResult,
                context,
                conversationHistory,
                cancellationToken);

            // Guardar mensaje saliente
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

            _logger.LogInformation("Respuesta generada para conversación: {ConversationId}", conversationId);

            return OrchestratorResult.Success(response, intentResult);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error procesando mensaje para conversación: {ConversationId}", conversationId);
            return OrchestratorResult.Failed($"Error interno: {ex.Message}");
        }
    }

    /// <summary>
    /// Clasifica la intención del mensaje del cliente.
    /// </summary>
    private async Task<IntentResult> ClassifyIntentAsync(
        string customerMessage,
        CancellationToken cancellationToken)
    {
        var plugin = GetRequiredPlugin(IntentClassificationPluginName);

        try
        {
            var result = await _kernel.InvokeAsync<IntentResult>(
                plugin["classify_intent"],
                new() { ["customerMessage"] = customerMessage },
                cancellationToken);

            return result ?? new IntentResult
            {
                Intent = IntentCategory.General,
                Confidence = 0.5,
                Reasoning = "No se pudo clasificar la intención"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clasificando intención, usando valor por defecto");
            return new IntentResult
            {
                Intent = IntentCategory.General,
                Confidence = 0.3,
                Reasoning = "Error en clasificación"
            };
        }
    }

    /// <summary>
    /// Obtiene contexto relevante de la base de conocimiento.
    /// </summary>
    private async Task<string> GetKnowledgeContextAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var plugin = GetRequiredPlugin(KnowledgeRetrievalPluginName);

        try
        {
            var context = await _kernel.InvokeAsync<string>(
                plugin["get_formatted_context"],
                new() { ["query"] = query, ["maxDocuments"] = 3 },
                cancellationToken);

            return context ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo contexto de conocimiento");
            return string.Empty;
        }
    }

    /// <summary>
    /// Construye el historial de conversación reciente.
    /// </summary>
    private async Task<string> BuildConversationHistoryAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var recentMessages = await _messageRepository.GetRecentMessagesAsync(conversationId, 5, cancellationToken);

        var historyBuilder = new System.Text.StringBuilder();
        foreach (var msg in recentMessages.OrderBy(m => m.CreatedAt))
        {
            var speaker = msg.IsFromBot ? "Agente" : "Cliente";
            historyBuilder.AppendLine($"{speaker}: {msg.Content}");
        }

        return historyBuilder.ToString();
    }

    /// <summary>
    /// Genera una respuesta usando el plugin de generación.
    /// </summary>
    private async Task<string> GenerateResponseAsync(
        string customerMessage,
        IntentResult intentResult,
        string context,
        string conversationHistory,
        CancellationToken cancellationToken)
    {
        var plugin = GetRequiredPlugin(ResponseGenerationPluginName);

        try
        {
            var response = await _kernel.InvokeAsync<string>(
                plugin["generate_response"],
                new()
                {
                    ["customerMessage"] = customerMessage,
                    ["intent"] = intentResult.Intent.ToString(),
                    ["context"] = context,
                    ["conversationHistory"] = conversationHistory
                },
                cancellationToken);

            return response ?? "Lo siento, no pude generar una respuesta. ¿Podría reformular su pregunta?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando respuesta");
            return "Disculpe, estamos experimentando dificultades técnicas. Por favor, intente de nuevo en unos momentos.";
        }
    }

    /// <summary>
    /// Crea una nueva conversación.
    /// </summary>
    public async Task<Conversation> CreateConversationAsync(
        string customerId,
        ChannelType channel,
        string? customerName = null,
        string? customerPhone = null,
        string? customerEmail = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId, nameof(customerId));

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

        _logger.LogInformation("Conversación creada: {ConversationId} para cliente: {CustomerId}",
            conversation.Id, customerId);

        return conversation;
    }

    /// <summary>
    /// Finaliza una conversación con resumen opcional.
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
            throw new InvalidOperationException($"Conversación no encontrada: {conversationId}");
        }

        conversation.Status = status;
        conversation.Resolution = resolution;
        conversation.EndedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        _logger.LogInformation("Conversación finalizada: {ConversationId} con estado: {Status}",
            conversationId, status);

        return conversation;
    }

    /// <summary>
    /// Determina si se debe escalar la conversación.
    /// </summary>
    private static bool ShouldEscalate(IntentResult intentResult)
    {
        // Escalar para quejas, emergencias, o baja confianza
        return intentResult.Intent == IntentCategory.Complaint ||
               intentResult.Intent == IntentCategory.Emergency ||
               intentResult.Confidence < 0.5;
    }

    /// <summary>
    /// Maneja la escalación de una conversación.
    /// </summary>
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
            IntentCategory.Emergency => "Entiendo que esto es urgente. Permítame conectarlo con un especialista inmediatamente.",
            IntentCategory.Complaint => "Lamento escuchar sobre su experiencia. Permítame transferirlo con un supervisor que pueda ayudarle a resolver esto.",
            _ => "Permítame conectarlo con un agente humano que pueda asistirle mejor con esta solicitud."
        };

        return OrchestratorResult.Escalated(escalationMessage, intentResult);
    }
}

/// <summary>
/// Resultado del proceso de orquestación de conversación.
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
