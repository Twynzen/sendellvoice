using Microsoft.Extensions.DependencyInjection;
using SendellVoice.Application.Plugins;
using SendellVoice.Application.Services;

namespace SendellVoice.Application;

/// <summary>
/// Extension methods for configuring Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Semantic Kernel plugins
        services.AddScoped<IntentClassificationPlugin>();
        services.AddScoped<ResponseGenerationPlugin>();
        services.AddScoped<KnowledgeRetrievalPlugin>();

        // Register application services
        services.AddScoped<ConversationOrchestrator>();
        services.AddScoped<SpeechToActionService>();
        services.AddScoped<RAGService>();

        return services;
    }
}
