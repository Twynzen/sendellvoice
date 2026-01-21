using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Application.Plugins;
using SendellVoice.Domain.Interfaces;
using SendellVoice.Infrastructure.AI;
using SendellVoice.Infrastructure.Configuration;
using SendellVoice.Infrastructure.Data;
using SendellVoice.Infrastructure.Data.Repositories;
using SendellVoice.Infrastructure.Documents;
using SendellVoice.Infrastructure.Speech;
using SendellVoice.Infrastructure.VectorStore;

namespace SendellVoice.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration options
        services.Configure<ProviderSettings>(configuration.GetSection("Providers"));
        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        services.Configure<OllamaSettings>(configuration.GetSection("Ollama"));
        services.Configure<AzureSpeechSettings>(configuration.GetSection("AzureSpeech"));
        services.Configure<WhisperSettings>(configuration.GetSection("WhisperNet"));
        services.Configure<AzureAISearchSettings>(configuration.GetSection("AzureAISearch"));
        services.Configure<QdrantSettings>(configuration.GetSection("Qdrant"));

        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();

        // Document processing
        services.AddSingleton<IDocumentProcessor, DocumentProcessor>();

        // Add provider-specific services
        var llmProvider = configuration["Providers:LLM"] ?? "Ollama";
        var speechProvider = configuration["Providers:Speech"] ?? "WhisperNet";
        var vectorProvider = configuration["Providers:VectorStore"] ?? "InMemory";

        // LLM and Embedding services
        AddLlmServices(services, llmProvider);
        AddEmbeddingServices(services, llmProvider);

        // Speech services
        AddSpeechServices(services, speechProvider);

        // Vector store services
        AddVectorStoreServices(services, vectorProvider);

        // Semantic Kernel
        AddSemanticKernel(services, configuration);

        return services;
    }

    private static void AddLlmServices(IServiceCollection services, string provider)
    {
        switch (provider)
        {
            case "AzureOpenAI":
                services.AddSingleton<ILlmService, AzureOpenAIService>();
                break;
            case "Ollama":
            default:
                services.AddSingleton<ILlmService, OllamaService>();
                break;
        }
    }

    private static void AddEmbeddingServices(IServiceCollection services, string provider)
    {
        switch (provider)
        {
            case "AzureOpenAI":
                services.AddSingleton<IEmbeddingService, AzureOpenAIService>();
                break;
            case "Ollama":
            default:
                services.AddSingleton<IEmbeddingService, OllamaService>();
                break;
        }
    }

    private static void AddSpeechServices(IServiceCollection services, string provider)
    {
        switch (provider)
        {
            case "AzureSpeech":
                services.AddSingleton<ISpeechToTextService, AzureSpeechService>();
                break;
            case "WhisperNet":
            default:
                services.AddSingleton<ISpeechToTextService, WhisperNetService>();
                break;
        }
    }

    private static void AddVectorStoreServices(IServiceCollection services, string provider)
    {
        switch (provider)
        {
            case "AzureAISearch":
                services.AddSingleton<IVectorStoreService, AzureAISearchService>();
                break;
            case "Qdrant":
                services.AddSingleton<IVectorStoreService, QdrantService>();
                break;
            case "InMemory":
            default:
                services.AddSingleton<IVectorStoreService, InMemoryVectorStore>();
                break;
        }
    }

    private static void AddSemanticKernel(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            // Add LLM services based on configuration
            builder.AddLlmServices(configuration);

            // Build the kernel
            var kernel = builder.Build();

            // Register plugins
            var intentPlugin = sp.GetRequiredService<IntentClassificationPlugin>();
            var responsePlugin = sp.GetRequiredService<ResponseGenerationPlugin>();
            var knowledgePlugin = sp.GetRequiredService<KnowledgeRetrievalPlugin>();

            kernel.Plugins.AddFromObject(intentPlugin, "IntentClassification");
            kernel.Plugins.AddFromObject(responsePlugin, "ResponseGeneration");
            kernel.Plugins.AddFromObject(knowledgePlugin, "KnowledgeRetrieval");

            return kernel;
        });
    }
}
