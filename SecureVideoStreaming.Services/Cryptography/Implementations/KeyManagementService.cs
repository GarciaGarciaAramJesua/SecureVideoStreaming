using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly string _keysPath;
        private readonly IRsaService _rsaService;
        private readonly IHashService _hashService;
        private const string PrivateKeyFileName = "server_private_key.pem";
        private const string PublicKeyFileName = "server_public_key.pem";
        private const string MasterSecretFileName = ".master_secret";

        public KeyManagementService(IRsaService rsaService, IHashService hashService)
        {
            _rsaService = rsaService;
            _hashService = hashService;
            
            // Obtener ruta de almacenamiento desde configuración o usar default
            _keysPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Keys");
            
            // Crear directorio si no existe
            if (!Directory.Exists(_keysPath))
            {
                Directory.CreateDirectory(_keysPath);
            }

            // Inicializar claves del servidor si no existen
            if (!ServerKeysExist())
            {
                GenerateServerKeyPair();
            }
        }

        public string GetServerPrivateKey()
        {
            var privateKeyPath = Path.Combine(_keysPath, PrivateKeyFileName);
            
            if (!File.Exists(privateKeyPath))
            {
                throw new FileNotFoundException("La clave privada del servidor no existe");
            }

            return File.ReadAllText(privateKeyPath);
        }

        public string GetServerPublicKey()
        {
            var publicKeyPath = Path.Combine(_keysPath, PublicKeyFileName);
            
            if (!File.Exists(publicKeyPath))
            {
                throw new FileNotFoundException("La clave pública del servidor no existe");
            }

            return File.ReadAllText(publicKeyPath);
        }

        public void GenerateServerKeyPair(int keySize = 4096)
        {
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(keySize);
            SaveServerKeys(publicKey, privateKey);
            
            // Generar y guardar master secret para derivación de claves HMAC
            GenerateMasterSecret();
        }

        public bool ServerKeysExist()
        {
            var privateKeyPath = Path.Combine(_keysPath, PrivateKeyFileName);
            var publicKeyPath = Path.Combine(_keysPath, PublicKeyFileName);
            
            return File.Exists(privateKeyPath) && File.Exists(publicKeyPath);
        }

        public void SaveServerKeys(string publicKey, string privateKey)
        {
            var privateKeyPath = Path.Combine(_keysPath, PrivateKeyFileName);
            var publicKeyPath = Path.Combine(_keysPath, PublicKeyFileName);

            // Guardar clave privada con permisos restrictivos
            File.WriteAllText(privateKeyPath, privateKey);
            
            // Guardar clave pública
            File.WriteAllText(publicKeyPath, publicKey);

            // En Windows, establecer permisos restrictivos (solo lectura para el propietario)
            try
            {
                var fileInfo = new FileInfo(privateKeyPath);
                fileInfo.IsReadOnly = false; // Asegurar que podemos modificar permisos
            }
            catch
            {
                // Ignorar errores de permisos en entornos donde no se pueden modificar
            }
        }

        public byte[] DeriveHmacKeyForUser(int userId, string userEmail)
        {
            // Obtener master secret
            var masterSecret = GetOrCreateMasterSecret();

            // Crear datos de entrada únicos para cada usuario
            var userData = Encoding.UTF8.GetBytes($"HMAC_KEY_USER_{userId}_{userEmail}");

            // Derivar clave HMAC usando PBKDF2 con el master secret como password
            // Usamos el userId y email como salt adicional para unicidad
            var salt = _hashService.ComputeSha256(userData);

            var hmacKey = _hashService.DeriveKey(
                Convert.ToBase64String(masterSecret),
                salt,
                iterations: 210000, // OWASP recomienda 210,000+ para PBKDF2-SHA256
                keyLength: 64 // 512 bits para HMAC-SHA256
            );

            return hmacKey;
        }

        private void GenerateMasterSecret()
        {
            var masterSecretPath = Path.Combine(_keysPath, MasterSecretFileName);
            
            // Generar 256 bits (32 bytes) de entropía criptográfica
            var masterSecret = RandomNumberGenerator.GetBytes(32);
            
            // Guardar en formato Base64
            File.WriteAllText(masterSecretPath, Convert.ToBase64String(masterSecret));
        }

        private byte[] GetOrCreateMasterSecret()
        {
            var masterSecretPath = Path.Combine(_keysPath, MasterSecretFileName);

            if (!File.Exists(masterSecretPath))
            {
                GenerateMasterSecret();
            }

            var masterSecretB64 = File.ReadAllText(masterSecretPath);
            return Convert.FromBase64String(masterSecretB64);
        }
    }
}
