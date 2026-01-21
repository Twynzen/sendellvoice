using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SendellVoice.Web.HealthChecks;

/// <summary>
/// Health check for Ollama service.
/// </summary>
public class OllamaHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaHealthCheck> _logger;

    public OllamaHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["Ollama:Endpoint"] ?? "http://localhost:11434";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"{endpoint}/api/tags", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Ollama is responding");
            }

            return HealthCheckResult.Degraded($"Ollama returned status: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Ollama health check timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed");
            return HealthCheckResult.Unhealthy("Ollama is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Ollama health check");
            return HealthCheckResult.Unhealthy("Unexpected error", ex);
        }
    }
}

/// <summary>
/// Health check for Qdrant service.
/// </summary>
public class QdrantHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantHealthCheck> _logger;

    public QdrantHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Qdrant:Host"] ?? "localhost";
        var port = _configuration["Qdrant:Port"] ?? "6333";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"http://{host}:{port}/collections", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Qdrant is responding");
            }

            return HealthCheckResult.Degraded($"Qdrant returned status: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Qdrant health check timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Qdrant health check failed");
            return HealthCheckResult.Unhealthy("Qdrant is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Qdrant health check");
            return HealthCheckResult.Unhealthy("Unexpected error", ex);
        }
    }
}

/// <summary>
/// Health check for the vector store service.
/// </summary>
public class VectorStoreHealthCheck : IHealthCheck
{
    private readonly Application.Common.Interfaces.IVectorStoreService _vectorStoreService;
    private readonly ILogger<VectorStoreHealthCheck> _logger;

    public VectorStoreHealthCheck(
        Application.Common.Interfaces.IVectorStoreService vectorStoreService,
        ILogger<VectorStoreHealthCheck> logger)
    {
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isAvailable = await _vectorStoreService.IsAvailableAsync(cancellationToken);

            return isAvailable
                ? HealthCheckResult.Healthy("Vector store is available")
                : HealthCheckResult.Unhealthy("Vector store is not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vector store health");
            return HealthCheckResult.Unhealthy("Vector store health check failed", ex);
        }
    }
}
