namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    /// <summary>
    /// Servicio para cifrado y gestión criptográfica de videos
    /// </summary>
    public interface IVideoEncryptionService
    {
        /// <summary>
        /// Cifra un archivo de video completamente
        /// </summary>
        /// <param name="inputFilePath">Ruta del video original</param>
        /// <param name="outputFilePath">Ruta donde se guardará el video cifrado</param>
        /// <param name="hmacKey">Clave HMAC del administrador para autenticación</param>
        /// <param name="serverPublicKey">Clave pública RSA del servidor para cifrar la KEK</param>
        /// <returns>Datos criptográficos del video (KEK cifrada, nonce, authTag, hashes, HMAC)</returns>
        Task<VideoEncryptionResult> EncryptVideoAsync(
            string inputFilePath,
            string outputFilePath,
            byte[] hmacKey,
            string serverPublicKey);

        /// <summary>
        /// Descifra un archivo de video
        /// </summary>
        /// <param name="encryptedFilePath">Ruta del video cifrado</param>
        /// <param name="outputFilePath">Ruta donde se guardará el video descifrado</param>
        /// <param name="kek">KEK descifrada (32 bytes)</param>
        /// <param name="nonce">Nonce usado en el cifrado (12 bytes)</param>
        /// <param name="authTag">Tag de autenticación (16 bytes)</param>
        Task DecryptVideoAsync(
            string encryptedFilePath,
            string outputFilePath,
            byte[] kek,
            byte[] nonce,
            byte[] authTag);

        /// <summary>
        /// Verifica la integridad de un video cifrado usando HMAC
        /// </summary>
        /// <param name="encryptedFilePath">Ruta del video cifrado</param>
        /// <param name="expectedHmac">HMAC esperado</param>
        /// <param name="hmacKey">Clave HMAC del administrador</param>
        bool VerifyVideoIntegrity(
            string encryptedFilePath,
            byte[] expectedHmac,
            byte[] hmacKey);

        /// <summary>
        /// Calcula el hash SHA-256 de un archivo
        /// </summary>
        byte[] CalculateFileHash(string filePath);
    }

    /// <summary>
    /// Resultado del cifrado de un video
    /// </summary>
    public class VideoEncryptionResult
    {
        public byte[] EncryptedKek { get; set; } = Array.Empty<byte>();
        public byte[] Nonce { get; set; } = Array.Empty<byte>();
        public byte[] AuthTag { get; set; } = Array.Empty<byte>();
        public byte[] OriginalHash { get; set; } = Array.Empty<byte>();
        public byte[] HmacOfEncryptedVideo { get; set; } = Array.Empty<byte>();
        public long OriginalSizeBytes { get; set; }
        public long EncryptedSizeBytes { get; set; }
    }
}
