using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SendellVoice.Infrastructure.Configuration;

namespace SendellVoice.Infrastructure.AI;

/// <summary>
/// Factory for configuring LLM services with Semantic Kernel.
/// </summary>
public static class LlmServiceFactory
{
    /// <summary>
    /// Adds LLM services to the Semantic Kernel builder based on configuration.
    /// </summary>
    public static IKernelBuilder AddLlmServices(this IKernelBuilder builder, IConfiguration config)
    {
        var provider = config["Providers:LLM"] ?? "Ollama";

        return provider switch
        {
            "AzureOpenAI" => AddAzureOpenAI(builder, config),
            "Ollama" => AddOllama(builder, config),
            "OpenAI" => AddOpenAI(builder, config),
            _ => throw new ArgumentException($"LLM provider '{provider}' is not supported")
        };
    }

    private static IKernelBuilder AddAzureOpenAI(IKernelBuilder builder, IConfiguration config)
    {
        var settings = config.GetSection("AzureOpenAI").Get<AzureOpenAISettings>()
            ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

        return builder.AddAzureOpenAIChatCompletion(
            settings.ChatDeployment,
            settings.Endpoint,
            settings.ApiKey);
    }

    private static IKernelBuilder AddOllama(IKernelBuilder builder, IConfiguration config)
    {
        var settings = config.GetSection("Ollama").Get<OllamaSettings>()
            ?? throw new InvalidOperationException("Ollama configuration is missing");

#pragma warning disable SKEXP0070 // Ollama connector is experimental
        return builder.AddOllamaChatCompletion(
            settings.Model,
            new Uri(settings.Endpoint));
#pragma warning restore SKEXP0070
    }

    private static IKernelBuilder AddOpenAI(IKernelBuilder builder, IConfiguration config)
    {
        var model = config["OpenAI:Model"] ?? "gpt-4o-mini";
        var apiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key is missing");

        return builder.AddOpenAIChatCompletion(model, apiKey);
    }
}
