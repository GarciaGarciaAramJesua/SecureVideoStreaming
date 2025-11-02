namespace SecureVideoStreaming.Services.Cryptography.Interfaces
{
    public interface IHashService
    {
        /// <summary>
        /// Calcula el hash SHA-256 de los datos
        /// </summary>
        byte[] ComputeSha256(byte[] data);

        /// <summary>
        /// Calcula el hash SHA-256 de un stream (útil para archivos grandes)
        /// </summary>
        byte[] ComputeSha256(Stream stream);

        /// <summary>
        /// Calcula el hash SHA-256 y lo retorna en formato hexadecimal
        /// </summary>
        string ComputeSha256Hex(byte[] data);

        /// <summary>
        /// Calcula el hash SHA-256 de un stream y lo retorna en formato hexadecimal
        /// </summary>
        string ComputeSha256Hex(Stream stream);

        /// <summary>
        /// Deriva una clave usando PBKDF2 con SHA-256
        /// </summary>
        /// <param name="password">Contraseña</param>
        /// <param name="salt">Salt (mínimo 16 bytes)</param>
        /// <param name="iterations">Número de iteraciones (mínimo 100,000)</param>
        /// <param name="keyLength">Longitud de la clave derivada en bytes</param>
        byte[] DeriveKey(string password, byte[] salt, int iterations = 100000, int keyLength = 32);

        /// <summary>
        /// Genera un salt aleatorio
        /// </summary>
        byte[] GenerateSalt(int length = 32);
    }
}