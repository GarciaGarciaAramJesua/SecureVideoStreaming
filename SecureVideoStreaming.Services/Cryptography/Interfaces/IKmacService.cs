namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    /// <summary>
    /// Servicio para KMAC (KECCAK Message Authentication Code) - SHA-3 based MAC
    /// </summary>
    public interface IKmacService
    {
        /// <summary>
        /// Calcula KMAC256
        /// </summary>
        /// <param name="data">Datos a autenticar</param>
        /// <param name="key">Clave secreta</param>
        /// <param name="customization">String de personalización (opcional)</param>
        /// <param name="outputLength">Longitud de salida en bytes (default 64)</param>
        byte[] ComputeKmac256(byte[] data, byte[] key, string customization = "", int outputLength = 64);

        /// <summary>
        /// Verifica un KMAC256
        /// </summary>
        bool VerifyKmac256(byte[] data, byte[] key, byte[] kmacToVerify, string customization = "");

        /// <summary>
        /// Genera una clave KMAC aleatoria
        /// </summary>
        byte[] GenerateKey(int length = 64);
    }
}
