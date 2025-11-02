using SecureVideoStreaming.Services.Cryptography.Implementations;
using System.Text;
using Xunit;

namespace SecureVideoStreaming.Tests.Cryptography
{
    public class HashServiceTests
    {
        private readonly HashService _service;

        public HashServiceTests()
        {
            _service = new HashService();
        }

        [Fact]
        public void ComputeSha256_ShouldReturnConsistentHash()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hola Mundo");

            // Act
            var hash1 = _service.ComputeSha256(data);
            var hash2 = _service.ComputeSha256(data);

            // Assert
            Assert.Equal(32, hash1.Length);
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ComputeSha256Hex_ShouldReturnHexString()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hola Mundo");

            // Act
            var hashHex = _service.ComputeSha256Hex(data);

            // Assert
            Assert.Equal(64, hashHex.Length); // 32 bytes = 64 hex chars
            Assert.Matches("^[0-9a-f]{64}$", hashHex);
        }

        [Fact]
        public void ComputeSha256_DifferentData_ShouldProduceDifferentHashes()
        {
            // Arrange
            var data1 = Encoding.UTF8.GetBytes("Hola Mundo");
            var data2 = Encoding.UTF8.GetBytes("Hola mundo");

            // Act
            var hash1 = _service.ComputeSha256(data1);
            var hash2 = _service.ComputeSha256(data2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GenerateSalt_ShouldReturnRandomBytes()
        {
            // Act
            var salt1 = _service.GenerateSalt(32);
            var salt2 = _service.GenerateSalt(32);

            // Assert
            Assert.Equal(32, salt1.Length);
            Assert.Equal(32, salt2.Length);
            Assert.NotEqual(salt1, salt2);
        }

        [Fact]
        public void DeriveKey_ShouldReturnConsistentKey()
        {
            // Arrange
            var password = "MiContraseñaSegura123!";
            var salt = _service.GenerateSalt();

            // Act
            var key1 = _service.DeriveKey(password, salt, 100000, 32);
            var key2 = _service.DeriveKey(password, salt, 100000, 32);

            // Assert
            Assert.Equal(32, key1.Length);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void DeriveKey_DifferentPasswords_ShouldProduceDifferentKeys()
        {
            // Arrange
            var password1 = "Contraseña1";
            var password2 = "Contraseña2";
            var salt = _service.GenerateSalt();

            // Act
            var key1 = _service.DeriveKey(password1, salt);
            var key2 = _service.DeriveKey(password2, salt);

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void DeriveKey_DifferentSalts_ShouldProduceDifferentKeys()
        {
            // Arrange
            var password = "MiContraseña";
            var salt1 = _service.GenerateSalt();
            var salt2 = _service.GenerateSalt();

            // Act
            var key1 = _service.DeriveKey(password, salt1);
            var key2 = _service.DeriveKey(password, salt2);

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void ComputeSha256_FromStream_ShouldWork()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hola Mundo desde un stream");
            using var stream = new MemoryStream(data);

            // Act
            var hash = _service.ComputeSha256(stream);

            // Assert
            Assert.Equal(32, hash.Length);
        }
    }
}