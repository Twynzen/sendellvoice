using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SendellVoice.Web.Configuration;

namespace SendellVoice.Web.Middleware;

/// <summary>
/// Middleware para autenticación por API key.
/// Implementa comparación en tiempo constante para prevenir ataques de timing.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    // Paths que no requieren autenticación
    private static readonly string[] ExcludedPaths = new[]
    {
        "/health",
        "/swagger",
        "/",
        "/favicon.ico"
    };

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<SecuritySettings> securityOptions)
    {
        var settings = securityOptions.Value;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Saltar autenticación para endpoints excluidos
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Verificar si la autenticación por API key está habilitada
        if (!settings.RequireApiKey || string.IsNullOrEmpty(settings.ApiKey))
        {
            await _next(context);
            return;
        }

        // Validar API key
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            _logger.LogWarning(
                "API key faltante para solicitud: {Method} {Path} desde {IP}",
                context.Request.Method,
                context.Request.Path,
                GetClientIp(context));

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Se requiere API key",
                code = "API_KEY_REQUIRED"
            });
            return;
        }

        // Comparación en tiempo constante para prevenir timing attacks
        if (!SecureCompare(settings.ApiKey, providedApiKey.ToString()))
        {
            _logger.LogWarning(
                "API key inválida para solicitud: {Method} {Path} desde {IP}",
                context.Request.Method,
                context.Request.Path,
                GetClientIp(context));

            // Añadir pequeño delay para dificultar ataques de fuerza bruta
            await Task.Delay(Random.Shared.Next(100, 300));

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "API key inválida",
                code = "INVALID_API_KEY"
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Verifica si el path está excluido de autenticación.
    /// </summary>
    private static bool IsExcludedPath(string path)
    {
        foreach (var excluded in ExcludedPaths)
        {
            if (path == excluded || path.StartsWith(excluded + "/"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Comparación de strings en tiempo constante para prevenir timing attacks.
    /// </summary>
    private static bool SecureCompare(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    /// <summary>
    /// Obtiene la IP del cliente considerando proxies.
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        // Verificar X-Forwarded-For para proxies/load balancers
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Tomar la primera IP (cliente original)
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Métodos de extensión para autenticación por API key.
/// </summary>
public static class ApiKeyAuthExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
