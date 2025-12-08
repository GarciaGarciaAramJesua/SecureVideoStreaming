using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    /// <summary>
    /// Implementación del servicio de cifrado de claves privadas RSA del cliente
    /// 
    /// Formato del ciphertext (JSON Base64):
    /// {
    ///   "version": 1,
    ///   "salt": "base64",
    ///   "nonce": "base64", 
    ///   "authTag": "base64",
    ///   "ciphertext": "base64"
    /// }
    /// 
    /// Proceso de cifrado:
    /// 1. Generar salt aleatorio de 32 bytes
    /// 2. Derivar clave de cifrado con PBKDF2-SHA256 (200,000 iteraciones, 32 bytes)
    /// 3. Cifrar clave privada PEM con ChaCha20-Poly1305
    /// 4. Serializar todo a JSON y codificar en Base64
    /// </summary>
    public class PrivateKeyEncryptionService : IPrivateKeyEncryptionService
    {
        private readonly IHashService _hashService;
        private readonly IChaCha20Poly1305Service _chaChaService;

        // Constantes de seguridad
        private const int SALT_SIZE = 32; // 256 bits
        private const int KEY_SIZE = 32;  // 256 bits
        private const int PBKDF2_ITERATIONS = 200000; // OWASP 2023 recomienda 600k, pero 200k es balance rendimiento/seguridad
        private const int CURRENT_VERSION = 1;

        public PrivateKeyEncryptionService(
            IHashService hashService,
            IChaCha20Poly1305Service chaChaService)
        {
            _hashService = hashService;
            _chaChaService = chaChaService;
        }

        public string EncryptPrivateKey(string privateKeyPem, string password)
        {
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new ArgumentException("La clave privada no puede estar vacía", nameof(privateKeyPem));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            // Validar formato PEM básico
            if (!privateKeyPem.Contains("-----BEGIN PRIVATE KEY-----"))
                throw new ArgumentException("Formato de clave privada PEM inválido", nameof(privateKeyPem));

            try
            {
                // 1. Generar salt aleatorio
                var salt = _hashService.GenerateSalt(SALT_SIZE);

                // 2. Derivar clave de cifrado con PBKDF2
                var encryptionKey = _hashService.DeriveKey(
                    password,
                    salt,
                    iterations: PBKDF2_ITERATIONS,
                    keyLength: KEY_SIZE);

                // 3. Convertir clave privada PEM a bytes
                var privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyPem);

                // 4. Cifrar con ChaCha20-Poly1305
                var (ciphertext, nonce, authTag) = _chaChaService.Encrypt(
                    plainData: privateKeyBytes,
                    key: encryptionKey,
                    nonce: null, // Se genera automáticamente
                    associatedData: salt); // Usar salt como AAD para vinculación

                // 5. Crear estructura de datos cifrados
                var encryptedData = new EncryptedPrivateKeyData
                {
                    Version = CURRENT_VERSION,
                    Salt = Convert.ToBase64String(salt),
                    Nonce = Convert.ToBase64String(nonce),
                    AuthTag = Convert.ToBase64String(authTag),
                    Ciphertext = Convert.ToBase64String(ciphertext)
                };

                // 6. Serializar a JSON y codificar en Base64
                var json = JsonSerializer.Serialize(encryptedData);
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                return Convert.ToBase64String(jsonBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al cifrar la clave privada", ex);
            }
        }

        public string DecryptPrivateKey(string encryptedPrivateKey, string password)
        {
            if (string.IsNullOrWhiteSpace(encryptedPrivateKey))
                throw new ArgumentException("Los datos cifrados no pueden estar vacíos", nameof(encryptedPrivateKey));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            try
            {
                // 1. Decodificar Base64 y deserializar JSON
                var jsonBytes = Convert.FromBase64String(encryptedPrivateKey);
                var json = Encoding.UTF8.GetString(jsonBytes);
                var encryptedData = JsonSerializer.Deserialize<EncryptedPrivateKeyData>(json);

                if (encryptedData == null)
                    throw new CryptographicException("Formato de datos cifrados inválido");

                // 2. Validar versión
                if (encryptedData.Version != CURRENT_VERSION)
                    throw new CryptographicException($"Versión de formato no soportada: {encryptedData.Version}");

                // 3. Decodificar componentes
                var salt = Convert.FromBase64String(encryptedData.Salt);
                var nonce = Convert.FromBase64String(encryptedData.Nonce);
                var authTag = Convert.FromBase64String(encryptedData.AuthTag);
                var ciphertext = Convert.FromBase64String(encryptedData.Ciphertext);

                // 4. Derivar clave de cifrado con PBKDF2 (mismos parámetros)
                var encryptionKey = _hashService.DeriveKey(
                    password,
                    salt,
                    iterations: PBKDF2_ITERATIONS,
                    keyLength: KEY_SIZE);

                // 5. Descifrar con ChaCha20-Poly1305
                var privateKeyBytes = _chaChaService.Decrypt(
                    cipherData: ciphertext,
                    key: encryptionKey,
                    nonce: nonce,
                    authTag: authTag,
                    associatedData: salt); // Usar salt como AAD

                // 6. Convertir bytes a string PEM
                var privateKeyPem = Encoding.UTF8.GetString(privateKeyBytes);

                // 7. Validar formato PEM del resultado
                if (!privateKeyPem.Contains("-----BEGIN PRIVATE KEY-----"))
                    throw new CryptographicException("La clave descifrada no tiene formato PEM válido");

                return privateKeyPem;
            }
            catch (CryptographicException)
            {
                // Re-lanzar excepciones criptográficas (contraseña incorrecta o datos modificados)
                throw;
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al descifrar la clave privada", ex);
            }
        }

        public bool ValidateEncryptedFormat(string encryptedPrivateKey)
        {
            if (string.IsNullOrWhiteSpace(encryptedPrivateKey))
                return false;

            try
            {
                // Intentar decodificar y deserializar
                var jsonBytes = Convert.FromBase64String(encryptedPrivateKey);
                var json = Encoding.UTF8.GetString(jsonBytes);
                var encryptedData = JsonSerializer.Deserialize<EncryptedPrivateKeyData>(json);

                if (encryptedData == null)
                    return false;

                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(encryptedData.Salt) ||
                    string.IsNullOrWhiteSpace(encryptedData.Nonce) ||
                    string.IsNullOrWhiteSpace(encryptedData.AuthTag) ||
                    string.IsNullOrWhiteSpace(encryptedData.Ciphertext))
                {
                    return false;
                }

                // Validar que se puedan decodificar de Base64
                Convert.FromBase64String(encryptedData.Salt);
                Convert.FromBase64String(encryptedData.Nonce);
                Convert.FromBase64String(encryptedData.AuthTag);
                Convert.FromBase64String(encryptedData.Ciphertext);

                // Validar versión conocida
                return encryptedData.Version == CURRENT_VERSION;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Estructura de datos para clave privada cifrada
        /// </summary>
        private class EncryptedPrivateKeyData
        {
            public int Version { get; set; }
            public string Salt { get; set; } = string.Empty;
            public string Nonce { get; set; } = string.Empty;
            public string AuthTag { get; set; } = string.Empty;
            public string Ciphertext { get; set; } = string.Empty;
        }
    }
}
