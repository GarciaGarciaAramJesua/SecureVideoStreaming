namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    public interface IChaCha20Poly1305Service
    {
        /// <summary>
        /// Cifra datos usando ChaCha20-Poly1305
        /// </summary>
        /// <param name="plainData">Datos a cifrar</param>
        /// <param name="key">Clave de 256 bits (32 bytes)</param>
        /// <param name="nonce">Nonce de 96 bits (12 bytes). Si es null, se genera automáticamente</param>
        /// <param name="associatedData">Datos adicionales autenticados (AAD) - opcional</param>
        /// <returns>Tupla con (datos cifrados, nonce usado, tag de autenticación)</returns>
        (byte[] ciphertext, byte[] nonce, byte[] authTag) Encrypt(
            byte[] plainData, 
            byte[] key, 
            byte[]? nonce = null, 
            byte[]? associatedData = null);

        /// <summary>
        /// Descifra datos usando ChaCha20-Poly1305
        /// </summary>
        /// <param name="cipherData">Datos cifrados</param>
        /// <param name="key">Clave de 256 bits (32 bytes)</param>
        /// <param name="nonce">Nonce de 96 bits (12 bytes)</param>
        /// <param name="authTag">Tag de autenticación de 128 bits (16 bytes)</param>
        /// <param name="associatedData">Datos adicionales autenticados (AAD) - opcional</param>
        /// <returns>Datos descifrados</returns>
        /// <exception cref="CryptographicException">Si la autenticación falla</exception>
        byte[] Decrypt(
            byte[] cipherData, 
            byte[] key, 
            byte[] nonce, 
            byte[] authTag, 
            byte[]? associatedData = null);

        /// <summary>
        /// Genera una clave aleatoria de 256 bits
        /// </summary>
        byte[] GenerateKey();

        /// <summary>
        /// Genera un nonce aleatorio de 96 bits
        /// </summary>
        byte[] GenerateNonce();
    }
}