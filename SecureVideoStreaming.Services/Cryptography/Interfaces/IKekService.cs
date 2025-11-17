namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    /// <summary>
    /// Servicio para gestión de KEKs (Key Encryption Keys)
    /// </summary>
    public interface IKekService
    {
        /// <summary>
        /// Genera una KEK aleatoria de 256 bits
        /// </summary>
        byte[] GenerateKek();

        /// <summary>
        /// Cifra una KEK usando la clave pública RSA del servidor
        /// </summary>
        /// <param name="kek">KEK a cifrar</param>
        /// <param name="serverPublicKey">Clave pública RSA del servidor en formato PEM</param>
        byte[] EncryptKek(byte[] kek, string serverPublicKey);

        /// <summary>
        /// Descifra una KEK usando la clave privada RSA del servidor
        /// </summary>
        /// <param name="encryptedKek">KEK cifrada</param>
        /// <param name="serverPrivateKey">Clave privada RSA del servidor en formato PEM</param>
        byte[] DecryptKek(byte[] encryptedKek, string serverPrivateKey);

        /// <summary>
        /// Genera una KEK, la cifra con RSA y retorna ambas
        /// </summary>
        /// <param name="serverPublicKey">Clave pública RSA del servidor</param>
        /// <returns>Tupla con (KEK en claro, KEK cifrada)</returns>
        (byte[] kek, byte[] encryptedKek) GenerateAndEncryptKek(string serverPublicKey);
    }
}
