namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    /// <summary>
    /// Servicio para cifrado/descifrado de claves privadas RSA del cliente
    /// Utiliza PBKDF2 para derivar clave de cifrado desde la contraseña del usuario
    /// y ChaCha20-Poly1305 para cifrado autenticado
    /// </summary>
    public interface IPrivateKeyEncryptionService
    {
        /// <summary>
        /// Cifra una clave privada RSA usando la contraseña del usuario
        /// </summary>
        /// <param name="privateKeyPem">Clave privada en formato PEM</param>
        /// <param name="password">Contraseña del usuario</param>
        /// <returns>Datos cifrados en formato Base64 que incluyen salt, nonce, tag y ciphertext</returns>
        string EncryptPrivateKey(string privateKeyPem, string password);

        /// <summary>
        /// Descifra una clave privada RSA usando la contraseña del usuario
        /// </summary>
        /// <param name="encryptedPrivateKey">Datos cifrados en formato Base64</param>
        /// <param name="password">Contraseña del usuario</param>
        /// <returns>Clave privada descifrada en formato PEM</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Si la contraseña es incorrecta o los datos han sido modificados
        /// </exception>
        string DecryptPrivateKey(string encryptedPrivateKey, string password);

        /// <summary>
        /// Valida el formato de una clave privada cifrada
        /// </summary>
        /// <param name="encryptedPrivateKey">Datos cifrados en formato Base64</param>
        /// <returns>True si el formato es válido</returns>
        bool ValidateEncryptedFormat(string encryptedPrivateKey);
    }
}
