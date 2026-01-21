namespace SendellVoice.Web.Configuration;

/// <summary>
/// Configuración para carga de archivos.
/// </summary>
public class FileUploadSettings
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Tamaño máximo de archivo en bytes. Por defecto 50MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52428800;

    /// <summary>
    /// Extensiones de archivo permitidas.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = new[]
    {
        ".pdf", ".docx", ".txt", ".md", ".html", ".htm"
    };
}

/// <summary>
/// Configuración de seguridad.
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "Security";

    /// <summary>
    /// API Key para autenticación.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Si se requiere API Key para acceder a los endpoints.
    /// </summary>
    public bool RequireApiKey { get; set; } = false;
}

/// <summary>
/// Configuración de rate limiting.
/// </summary>
public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Número máximo de solicitudes permitidas en la ventana de tiempo.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Ventana de tiempo en segundos.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
