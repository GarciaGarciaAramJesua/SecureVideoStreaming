using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    /// <summary>
    /// Implementación de KMAC256 usando SHA3-256 (simulación con HMAC-SHA256)
    /// NOTA: .NET no tiene soporte nativo para KMAC. Esta es una implementación basada en HMAC-SHA256
    /// Para una implementación real de KMAC, se requeriría BouncyCastle con soporte SHA-3
    /// </summary>
    public class KmacService : IKmacService
    {
        public byte[] ComputeKmac256(byte[] data, byte[] key, string customization = "", int outputLength = 64)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (key == null || key.Length == 0)
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            if (outputLength < 16 || outputLength > 128)
                throw new ArgumentException("La longitud de salida debe estar entre 16 y 128 bytes", nameof(outputLength));

            // Preparar datos con customización
            var customBytes = Encoding.UTF8.GetBytes(customization);
            var dataWithCustom = new byte[data.Length + customBytes.Length];
            Buffer.BlockCopy(customBytes, 0, dataWithCustom, 0, customBytes.Length);
            Buffer.BlockCopy(data, 0, dataWithCustom, customBytes.Length, data.Length);

            // Calcular HMAC-SHA256 (aproximación de KMAC)
            using var hmac = new HMACSHA256(key);
            var mac = hmac.ComputeHash(dataWithCustom);

            // Si se requiere más longitud, usar derivación de claves
            if (outputLength > mac.Length)
            {
                using var deriveBytes = new Rfc2898DeriveBytes(
                    mac,
                    key,
                    1000,
                    HashAlgorithmName.SHA256);
                
                return deriveBytes.GetBytes(outputLength);
            }

            // Si se requiere menos, truncar
            if (outputLength < mac.Length)
            {
                var truncated = new byte[outputLength];
                Buffer.BlockCopy(mac, 0, truncated, 0, outputLength);
                return truncated;
            }

            return mac;
        }

        public bool VerifyKmac256(byte[] data, byte[] key, byte[] kmacToVerify, string customization = "")
        {
            if (kmacToVerify == null || kmacToVerify.Length == 0)
                throw new ArgumentException("El KMAC a verificar no puede estar vacío", nameof(kmacToVerify));

            var computedKmac = ComputeKmac256(data, key, customization, kmacToVerify.Length);

            // Comparación de tiempo constante para evitar timing attacks
            return CryptographicOperations.FixedTimeEquals(computedKmac, kmacToVerify);
        }

        public byte[] GenerateKey(int length = 64)
        {
            if (length < 32 || length > 128)
                throw new ArgumentException("La longitud de la clave debe estar entre 32 y 128 bytes", nameof(length));

            return RandomNumberGenerator.GetBytes(length);
        }
    }
}
