namespace SecureVideoStreaming.Models.DTOs.Response
{
    /// <summary>
    /// Paquete de claves criptográficas para consumidor
    /// </summary>
    public class KeyPackageResponse
    {
        /// <summary>
        /// KEK cifrada con la clave pública del consumidor (RSA-OAEP)
        /// </summary>
        public string EncryptedKekForUser { get; set; } = string.Empty;

        /// <summary>
        /// Nonce usado en el cifrado ChaCha20-Poly1305 (12 bytes, Base64)
        /// </summary>
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// Authentication Tag de Poly1305 (16 bytes, Base64)
        /// </summary>
        public string AuthTag { get; set; } = string.Empty;

        /// <summary>
        /// Algoritmo de cifrado usado para el video
        /// </summary>
        public string Algorithm { get; set; } = "ChaCha20-Poly1305";

        /// <summary>
        /// ID del video asociado
        /// </summary>
        public int VideoId { get; set; }

        /// <summary>
        /// Token para streaming
        /// </summary>
        public string? StreamingToken { get; set; }

        /// <summary>
        /// Timestamp de generación del paquete
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Expiración del paquete (1 hora por defecto)
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Token de streaming temporal
    /// </summary>
    public class StreamingTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public int VideoId { get; set; }
        public string StreamingUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = "video/mp4";
    }
}
