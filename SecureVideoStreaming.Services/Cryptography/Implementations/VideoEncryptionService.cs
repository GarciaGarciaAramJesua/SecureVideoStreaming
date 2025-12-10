using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class VideoEncryptionService : IVideoEncryptionService
    {
        private readonly IChaCha20Poly1305Service _chaChaService;
        private readonly IHashService _hashService;
        private readonly IHmacService _hmacService;
        private readonly IKekService _kekService;
        private const int BufferSize = 8192; // 8 KB buffer para procesamiento por bloques

        public VideoEncryptionService(
            IChaCha20Poly1305Service chaChaService,
            IHashService hashService,
            IHmacService hmacService,
            IKekService kekService)
        {
            _chaChaService = chaChaService;
            _hashService = hashService;
            _hmacService = hmacService;
            _kekService = kekService;
        }

        public async Task<VideoEncryptionResult> EncryptVideoAsync(
            string inputFilePath,
            string outputFilePath,
            byte[] hmacKey,
            string serverPublicKey)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("El archivo de video no existe", inputFilePath);
            }

            if (hmacKey == null || hmacKey.Length == 0)
            {
                throw new ArgumentException("La clave HMAC no puede estar vacía", nameof(hmacKey));
            }

            // 1. Calcular hash SHA-256 del video original
            byte[] originalHash;
            using (var fileStream = File.OpenRead(inputFilePath))
            {
                originalHash = _hashService.ComputeSha256(fileStream);
            }

            var originalSize = new FileInfo(inputFilePath).Length;

            // 2. Generar KEK aleatoria de 256 bits
            var kek = _kekService.GenerateKek();

            // 3. Cifrar KEK con RSA del servidor
            var encryptedKek = _kekService.EncryptKek(kek, serverPublicKey);

            // 4. Cifrar el video con ChaCha20-Poly1305
            byte[] nonce;
            byte[] authTag;
            long encryptedSize;

            using (var inputStream = File.OpenRead(inputFilePath))
            {
                // Leer todo el archivo en memoria para cifrado
                // Nota: Para archivos muy grandes, se debería implementar cifrado por bloques
                var videoData = new byte[inputStream.Length];
                await inputStream.ReadAsync(videoData, 0, videoData.Length);

                // Cifrar con ChaCha20-Poly1305
                var (ciphertext, nonceGenerated, authTagGenerated) = _chaChaService.Encrypt(
                    videoData,
                    kek,
                    nonce: null, // Generar nonce automáticamente
                    associatedData: null
                );

                nonce = nonceGenerated;
                authTag = authTagGenerated;

                // Convertir datos cifrados a Base64 y guardar
                var ciphertextBase64 = Convert.ToBase64String(ciphertext);
                await File.WriteAllTextAsync(outputFilePath, ciphertextBase64);
                encryptedSize = ciphertext.Length;
                
                Console.WriteLine($"[VideoEncryptionService] Video cifrado guardado en Base64 ({ciphertextBase64.Length} caracteres)");
            }

            // 5. Calcular HMAC del video cifrado con clave del administrador
            byte[] hmacOfEncryptedVideo;
            using (var encryptedStream = File.OpenRead(outputFilePath))
            {
                hmacOfEncryptedVideo = _hmacService.ComputeHmac(encryptedStream, hmacKey);
            }

            // 6. Guardar metadata (nonce y authTag) en archivo separado
            var metadataPath = outputFilePath + ".metadata";
            var metadataContent = $"Nonce:{Convert.ToBase64String(nonce)}\nAuthTag:{Convert.ToBase64String(authTag)}";
            await File.WriteAllTextAsync(metadataPath, metadataContent);
            
            Console.WriteLine($"[VideoEncryptionService] Metadata guardada en: {metadataPath}");

            return new VideoEncryptionResult
            {
                EncryptedKek = encryptedKek,
                Nonce = nonce,
                AuthTag = authTag,
                OriginalHash = originalHash,
                HmacOfEncryptedVideo = hmacOfEncryptedVideo,
                OriginalSizeBytes = originalSize,
                EncryptedSizeBytes = encryptedSize
            };
        }

        public async Task DecryptVideoAsync(
            string encryptedFilePath,
            string outputFilePath,
            byte[] kek,
            byte[] nonce,
            byte[] authTag)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException("El archivo cifrado no existe", encryptedFilePath);
            }

            if (kek == null || kek.Length != 32)
            {
                throw new ArgumentException("La KEK debe tener 32 bytes", nameof(kek));
            }

            if (nonce == null || nonce.Length != 12)
            {
                throw new ArgumentException("El nonce debe tener 12 bytes", nameof(nonce));
            }

            if (authTag == null || authTag.Length != 16)
            {
                throw new ArgumentException("El authTag debe tener 16 bytes", nameof(authTag));
            }

            using (var outputStream = File.Create(outputFilePath))
            {
                // Leer datos cifrados desde Base64
                var encryptedBase64 = await File.ReadAllTextAsync(encryptedFilePath);
                var encryptedData = Convert.FromBase64String(encryptedBase64);
                
                Console.WriteLine($"[VideoEncryptionService] Video leído desde Base64 ({encryptedData.Length} bytes)");

                // Descifrar con ChaCha20-Poly1305
                var plaintext = _chaChaService.Decrypt(
                    encryptedData,
                    kek,
                    nonce,
                    authTag,
                    associatedData: null
                );

                // Escribir datos descifrados
                await outputStream.WriteAsync(plaintext, 0, plaintext.Length);
            }
        }

        public bool VerifyVideoIntegrity(
            string encryptedFilePath,
            byte[] expectedHmac,
            byte[] hmacKey)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException("El archivo cifrado no existe", encryptedFilePath);
            }

            if (expectedHmac == null || expectedHmac.Length == 0)
            {
                throw new ArgumentException("El HMAC esperado no puede estar vacío", nameof(expectedHmac));
            }

            if (hmacKey == null || hmacKey.Length == 0)
            {
                throw new ArgumentException("La clave HMAC no puede estar vacía", nameof(hmacKey));
            }

            // Leer archivo cifrado desde Base64 y calcular HMAC sobre los bytes
            var encryptedBase64 = File.ReadAllText(encryptedFilePath);
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            
            using (var memoryStream = new MemoryStream(encryptedBytes))
            {
                var computedHmac = _hmacService.ComputeHmac(memoryStream, hmacKey);
                return CryptographicOperations.FixedTimeEquals(computedHmac, expectedHmac);
            }
        }

        public byte[] CalculateFileHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("El archivo no existe", filePath);
            }

            using (var fileStream = File.OpenRead(filePath))
            {
                return _hashService.ComputeSha256(fileStream);
            }
        }
    }
}
