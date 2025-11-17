using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class KekService : IKekService
    {
        private readonly IRsaService _rsaService;

        public KekService(IRsaService rsaService)
        {
            _rsaService = rsaService;
        }

        public byte[] GenerateKek()
        {
            // Generar KEK de 256 bits (32 bytes) para ChaCha20-Poly1305
            return RandomNumberGenerator.GetBytes(32);
        }

        public byte[] EncryptKek(byte[] kek, string serverPublicKey)
        {
            if (kek == null || kek.Length != 32)
            {
                throw new ArgumentException("La KEK debe tener exactamente 32 bytes (256 bits)", nameof(kek));
            }

            if (string.IsNullOrWhiteSpace(serverPublicKey))
            {
                throw new ArgumentException("La clave pública del servidor no puede estar vacía", nameof(serverPublicKey));
            }

            return _rsaService.Encrypt(kek, serverPublicKey);
        }

        public byte[] DecryptKek(byte[] encryptedKek, string serverPrivateKey)
        {
            if (encryptedKek == null || encryptedKek.Length == 0)
            {
                throw new ArgumentException("La KEK cifrada no puede estar vacía", nameof(encryptedKek));
            }

            if (string.IsNullOrWhiteSpace(serverPrivateKey))
            {
                throw new ArgumentException("La clave privada del servidor no puede estar vacía", nameof(serverPrivateKey));
            }

            var kek = _rsaService.Decrypt(encryptedKek, serverPrivateKey);

            if (kek.Length != 32)
            {
                throw new CryptographicException("La KEK descifrada no tiene el tamaño esperado (32 bytes)");
            }

            return kek;
        }

        public (byte[] kek, byte[] encryptedKek) GenerateAndEncryptKek(string serverPublicKey)
        {
            var kek = GenerateKek();
            var encryptedKek = EncryptKek(kek, serverPublicKey);
            
            return (kek, encryptedKek);
        }
    }
}
