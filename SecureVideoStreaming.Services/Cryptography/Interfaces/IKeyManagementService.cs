namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    /// <summary>
    /// Servicio para gestión segura de claves privadas del servidor
    /// </summary>
    public interface IKeyManagementService
    {
        /// <summary>
        /// Obtiene la clave privada RSA del servidor
        /// </summary>
        string GetServerPrivateKey();

        /// <summary>
        /// Obtiene la clave pública RSA del servidor
        /// </summary>
        string GetServerPublicKey();

        /// <summary>
        /// Genera y almacena un nuevo par de claves RSA para el servidor
        /// </summary>
        /// <param name="keySize">Tamaño de la clave (2048 o 4096)</param>
        void GenerateServerKeyPair(int keySize = 4096);

        /// <summary>
        /// Verifica si existen las claves del servidor
        /// </summary>
        bool ServerKeysExist();

        /// <summary>
        /// Almacena de forma segura las claves del servidor en disco
        /// </summary>
        void SaveServerKeys(string publicKey, string privateKey);

        /// <summary>
        /// Deriva una clave HMAC única para un usuario específico
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="userEmail">Email del usuario para mayor entropía</param>
        byte[] DeriveHmacKeyForUser(int userId, string userEmail);
    }
}
