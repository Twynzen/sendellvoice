namespace SendellVoice.Application.Common.Interfaces;

/// <summary>
/// Interface for Language Model service operations.
/// </summary>
public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<TResult> GenerateStructuredResponseAsync<TResult>(string prompt, CancellationToken cancellationToken = default) where TResult : class;
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
