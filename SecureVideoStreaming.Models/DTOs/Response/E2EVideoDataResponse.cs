namespace SecureVideoStreaming.Models.DTOs.Response
{
    /// <summary>
    /// Respuesta E2E: Video cifrado + KEK cifrada para descifrado en cliente
    /// Compatible con @stablelib/chacha20poly1305
    /// </summary>
    public class E2EVideoDataResponse
    {
        /// <summary>
        /// Video cifrado con ChaCha20-Poly1305 (Base64)
        /// </summary>
        public string EncryptedVideo { get; set; } = string.Empty;

        /// <summary>
        /// KEK cifrada con la clave pública RSA del cliente (Base64)
        /// El cliente la descifrará con su clave privada efímera
        /// </summary>
        public string EncryptedKEK { get; set; } = string.Empty;

        /// <summary>
        /// Nonce de 96 bits usado en ChaCha20-Poly1305 (Base64)
        /// </summary>
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// Tag de autenticación de 128 bits (Base64)
        /// </summary>
        public string AuthTag { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño original del video (bytes)
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Formato del video (mp4, webm, etc)
        /// </summary>
        public string Format { get; set; } = "mp4";
    }
}
