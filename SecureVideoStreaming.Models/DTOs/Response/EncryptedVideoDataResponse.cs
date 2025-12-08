namespace SecureVideoStreaming.Models.DTOs.Response;

/// <summary>
/// Respuesta con los datos del video cifrado para descifrado en el cliente
/// </summary>
public class EncryptedVideoDataResponse
{
    /// <summary>
    /// Video cifrado en Base64
    /// </summary>
    public string Ciphertext { get; set; } = string.Empty;

    /// <summary>
    /// Nonce usado para el cifrado en Base64
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Tag de autenticación en Base64
    /// </summary>
    public string AuthTag { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del video original en bytes
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// Formato del video (mp4, webm, etc.)
    /// </summary>
    public string Format { get; set; } = string.Empty;
}
