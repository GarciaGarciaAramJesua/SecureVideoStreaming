using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class HmacService : IHmacService
    {
        public byte[] ComputeHmac(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (key == null || key.Length == 0)
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        public byte[] ComputeHmac(Stream stream, byte[] key)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("El stream debe ser legible", nameof(stream));

            if (key == null || key.Length == 0)
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(stream);
        }

        public string ComputeHmacHex(byte[] data, byte[] key)
        {
            var hmac = ComputeHmac(data, key);
            return Convert.ToHexString(hmac).ToLowerInvariant();
        }

        public bool VerifyHmac(byte[] data, byte[] key, byte[] hmacToVerify)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (key == null || key.Length == 0)
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            if (hmacToVerify == null || hmacToVerify.Length == 0)
                throw new ArgumentException("El HMAC a verificar no puede estar vacío", nameof(hmacToVerify));

            var computedHmac = ComputeHmac(data, key);

            // Comparación de tiempo constante para evitar timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHmac, hmacToVerify);
        }

        public byte[] GenerateKey(int length = 64)
        {
            if (length < 32 || length > 128)
                throw new ArgumentException("La longitud de la clave debe estar entre 32 y 128 bytes", nameof(length));

            return RandomNumberGenerator.GetBytes(length);
        }
    }
}