namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    public interface IRsaService
    {
        /// <summary>
        /// Genera un par de claves RSA (pública y privada)
        /// </summary>
        /// <param name="keySize">Tamaño de la clave (2048 o 4096 bits recomendado)</param>
        /// <returns>Tupla con (clave pública PEM, clave privada PEM)</returns>
        (string publicKey, string privateKey) GenerateKeyPair(int keySize = 2048);

        /// <summary>
        /// Cifra datos con la clave pública RSA usando OAEP
        /// </summary>
        /// <param name="data">Datos a cifrar (máximo ~214 bytes para RSA-2048)</param>
        /// <param name="publicKeyPem">Clave pública en formato PEM</param>
        /// <returns>Datos cifrados</returns>
        byte[] Encrypt(byte[] data, string publicKeyPem);

        /// <summary>
        /// Descifra datos con la clave privada RSA usando OAEP
        /// </summary>
        /// <param name="encryptedData">Datos cifrados</param>
        /// <param name="privateKeyPem">Clave privada en formato PEM</param>
        /// <returns>Datos descifrados</returns>
        byte[] Decrypt(byte[] encryptedData, string privateKeyPem);

        /// <summary>
        /// Firma datos con la clave privada RSA
        /// </summary>
        byte[] Sign(byte[] data, string privateKeyPem);

        /// <summary>
        /// Verifica la firma de datos con la clave pública RSA
        /// </summary>
        bool VerifySignature(byte[] data, byte[] signature, string publicKeyPem);
    }
}