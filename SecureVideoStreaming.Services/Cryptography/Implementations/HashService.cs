using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
public class HashService : IHashService
{
public byte[] ComputeSha256(byte[] data)
{
if (data == null || data.Length == 0)
throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));
        return SHA256.HashData(data);
    }

    public byte[] ComputeSha256(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("El stream debe ser legible", nameof(stream));

        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    public string ComputeSha256Hex(byte[] data)
    {
        var hash = ComputeSha256(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ComputeSha256Hex(Stream stream)
    {
        var hash = ComputeSha256(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public byte[] DeriveKey(string password, byte[] salt, int iterations = 100000, int keyLength = 32)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

        if (salt == null || salt.Length < 16)
            throw new ArgumentException("El salt debe tener al menos 16 bytes", nameof(salt));

        if (iterations < 100000)
            throw new ArgumentException("El número de iteraciones debe ser al menos 100,000", nameof(iterations));

        if (keyLength < 16 || keyLength > 64)
            throw new ArgumentException("La longitud de la clave debe estar entre 16 y 64 bytes", nameof(keyLength));

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(keyLength);
    }

    public byte[] GenerateSalt(int length = 32)
    {
        if (length < 16 || length > 128)
            throw new ArgumentException("La longitud del salt debe estar entre 16 y 128 bytes", nameof(length));

        return RandomNumberGenerator.GetBytes(length);
    }
}
}