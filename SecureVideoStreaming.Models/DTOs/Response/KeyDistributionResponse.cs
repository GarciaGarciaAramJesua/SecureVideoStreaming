namespace SecureVideoStreaming.Models.DTOs.Response
{
    /// <summary>
    /// Response con información de distribución de claves para un video
    /// </summary>
    public class KeyDistributionResponse
    {
        public int IdVideo { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        
        /// <summary>
        /// KEK cifrada con la clave pública del usuario
        /// Formato: Base64
        /// </summary>
        public string KEKCifradaParaUsuario { get; set; } = string.Empty;
        
        /// <summary>
        /// Nonce usado en el cifrado ChaCha20-Poly1305
        /// Formato: Base64
        /// </summary>
        public string Nonce { get; set; } = string.Empty;
        
        /// <summary>
        /// Tag de autenticación Poly1305
        /// Formato: Base64
        /// </summary>
        public string AuthTag { get; set; } = string.Empty;
        
        /// <summary>
        /// Algoritmo usado para cifrar el video
        /// </summary>
        public string AlgoritmoCifrado { get; set; } = string.Empty;
        
        /// <summary>
        /// Hash SHA-256 del video original (para verificación de integridad)
        /// Formato: Base64
        /// </summary>
        public string HashOriginal { get; set; } = string.Empty;
        
        /// <summary>
        /// HMAC del video cifrado (para verificación de autoría)
        /// Formato: Base64
        /// </summary>
        public string HMAC { get; set; } = string.Empty;
        
        /// <summary>
        /// URL para descargar el video cifrado
        /// </summary>
        public string VideoDownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Tamaño del archivo cifrado
        /// </summary>
        public long TamañoArchivo { get; set; }
        
        /// <summary>
        /// Timestamp de generación de esta distribución
        /// </summary>
        public DateTime FechaGeneracion { get; set; }
        
        /// <summary>
        /// ID del permiso asociado
        /// </summary>
        public int IdPermiso { get; set; }
    }
}
