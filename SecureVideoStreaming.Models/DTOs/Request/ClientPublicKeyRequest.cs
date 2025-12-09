namespace SecureVideoStreaming.Models.DTOs.Request
{
    /// <summary>
    /// Request para recibir la clave pública efímera del cliente (E2E)
    /// </summary>
    public class ClientPublicKeyRequest
    {
        /// <summary>
        /// Clave pública RSA del cliente en formato SPKI Base64
        /// Generada en el navegador con Web Crypto API
        /// </summary>
        public string ClientPublicKey { get; set; } = string.Empty;

        /// <summary>
        /// ID del usuario (opcional, se puede obtener del token JWT)
        /// </summary>
        public int? UserId { get; set; }
    }
}
