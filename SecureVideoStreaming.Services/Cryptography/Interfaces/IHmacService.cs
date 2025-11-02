namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    public interface IHmacService
    {
        /// <summary>
        /// Calcula HMAC-SHA256
        /// </summary>
        byte[] ComputeHmac(byte[] data, byte[] key);

        /// <summary>
        /// Calcula HMAC-SHA256 de un stream
        /// </summary>
        byte[] ComputeHmac(Stream stream, byte[] key);

        /// <summary>
        /// Calcula HMAC-SHA256 y lo retorna en formato hexadecimal
        /// </summary>
        string ComputeHmacHex(byte[] data, byte[] key);

        /// <summary>
        /// Verifica un HMAC
        /// </summary>
        bool VerifyHmac(byte[] data, byte[] key, byte[] hmacToVerify);

        /// <summary>
        /// Genera una clave HMAC aleatoria
        /// </summary>
        byte[] GenerateKey(int length = 64);
    }
}