namespace SendellVoice.Web.Middleware;

/// <summary>
/// Middleware for API key authentication.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health endpoints and Swagger
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path == "/" ||
            path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        // Check if API key authentication is enabled
        var apiKey = _configuration["ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            // No API key configured, skip authentication
            await _next(context);
            return;
        }

        // Validate API key
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            _logger.LogWarning("API key missing for request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }

        if (!apiKey.Equals(providedApiKey))
        {
            _logger.LogWarning("Invalid API key provided for request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for API key authentication.
/// </summary>
public static class ApiKeyAuthExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
