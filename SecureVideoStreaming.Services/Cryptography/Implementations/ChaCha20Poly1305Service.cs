using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class ChaCha20Poly1305Service : IChaCha20Poly1305Service
    {
        private const int KEY_SIZE = 32; // 256 bits
        private const int NONCE_SIZE = 12; // 96 bits
        private const int TAG_SIZE = 16; // 128 bits

        public (byte[] ciphertext, byte[] nonce, byte[] authTag) Encrypt(
            byte[] plainData, 
            byte[] key, 
            byte[]? nonce = null, 
            byte[]? associatedData = null)
        {
            if (plainData == null || plainData.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(plainData));

            if (key == null || key.Length != KEY_SIZE)
                throw new ArgumentException($"La clave debe tener {KEY_SIZE} bytes", nameof(key));

            // Generar nonce si no se proporciona
            if (nonce == null)
            {
                nonce = GenerateNonce();
            }
            else if (nonce.Length != NONCE_SIZE)
            {
                throw new ArgumentException($"El nonce debe tener {NONCE_SIZE} bytes", nameof(nonce));
            }

            try
            {
                // Crear buffers para ciphertext y tag
                var ciphertext = new byte[plainData.Length];
                var authTag = new byte[TAG_SIZE];

                // Usar ChaCha20Poly1305 nativo de .NET
                using var cipher = new System.Security.Cryptography.ChaCha20Poly1305(key);
                
                cipher.Encrypt(
                    nonce: nonce,
                    plaintext: plainData,
                    ciphertext: ciphertext,
                    tag: authTag,
                    associatedData: associatedData ?? Array.Empty<byte>());

                return (ciphertext, nonce, authTag);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al cifrar con ChaCha20-Poly1305", ex);
            }
        }

        public byte[] Decrypt(
            byte[] cipherData, 
            byte[] key, 
            byte[] nonce, 
            byte[] authTag, 
            byte[]? associatedData = null)
        {
            if (cipherData == null || cipherData.Length == 0)
                throw new ArgumentException("Los datos cifrados no pueden estar vacíos", nameof(cipherData));

            if (key == null || key.Length != KEY_SIZE)
                throw new ArgumentException($"La clave debe tener {KEY_SIZE} bytes", nameof(key));

            if (nonce == null || nonce.Length != NONCE_SIZE)
                throw new ArgumentException($"El nonce debe tener {NONCE_SIZE} bytes", nameof(nonce));

            if (authTag == null || authTag.Length != TAG_SIZE)
                throw new ArgumentException($"El tag de autenticación debe tener {TAG_SIZE} bytes", nameof(authTag));

            try
            {
                // Crear buffer para plaintext
                var plaintext = new byte[cipherData.Length];

                // Usar ChaCha20Poly1305 nativo de .NET
                using var cipher = new System.Security.Cryptography.ChaCha20Poly1305(key);
                
                cipher.Decrypt(
                    nonce: nonce,
                    ciphertext: cipherData,
                    tag: authTag,
                    plaintext: plaintext,
                    associatedData: associatedData ?? Array.Empty<byte>());

                return plaintext;
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Error de autenticación: los datos han sido modificados o la clave/nonce son incorrectos", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al descifrar con ChaCha20-Poly1305", ex);
            }
        }

        public byte[] GenerateKey()
        {
            return RandomNumberGenerator.GetBytes(KEY_SIZE);
        }

        public byte[] GenerateNonce()
        {
            return RandomNumberGenerator.GetBytes(NONCE_SIZE);
        }
    }
}