namespace SendellVoice.Application.Common.Interfaces;

/// <summary>
/// Interface for text embedding generation services.
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    int GetDimensions();
}
