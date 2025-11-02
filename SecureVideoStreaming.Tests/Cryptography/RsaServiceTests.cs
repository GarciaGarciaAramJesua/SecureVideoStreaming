using SecureVideoStreaming.Services.Cryptography.Implementations;
using System.Text;
using Xunit;

namespace SecureVideoStreaming.Tests.Cryptography
{
    public class RsaServiceTests
    {
        private readonly RsaService _service;

        public RsaServiceTests()
        {
            _service = new RsaService();
        }

        [Fact]
        public void GenerateKeyPair_ShouldReturnValidKeys()
        {
            // Act
            var (publicKey, privateKey) = _service.GenerateKeyPair(2048);

            // Assert
            Assert.NotNull(publicKey);
            Assert.NotNull(privateKey);
            Assert.Contains("BEGIN PUBLIC KEY", publicKey);
            Assert.Contains("BEGIN PRIVATE KEY", privateKey);
        }

        [Fact]
        public void Encrypt_Decrypt_ShouldReturnOriginalData()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Datos secretos");
            var (publicKey, privateKey) = _service.GenerateKeyPair(2048);

            // Act
            var encrypted = _service.Encrypt(data, publicKey);
            var decrypted = _service.Decrypt(encrypted, privateKey);

            // Assert
            Assert.Equal(data, decrypted);
            Assert.Equal("Datos secretos", Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentCiphertexts()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Datos secretos");
            var (publicKey, _) = _service.GenerateKeyPair(2048);

            // Act
            var encrypted1 = _service.Encrypt(data, publicKey);
            var encrypted2 = _service.Encrypt(data, publicKey);

            // Assert - RSA con padding OAEP es probabilístico
            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public void Decrypt_WithWrongKey_ShouldThrowException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Datos secretos");
            var (publicKey1, _) = _service.GenerateKeyPair(2048);
            var (_, privateKey2) = _service.GenerateKeyPair(2048);

            var encrypted = _service.Encrypt(data, publicKey1);

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            {
                _service.Decrypt(encrypted, privateKey2);
            });
        }

        [Fact]
        public void Sign_Verify_ShouldReturnTrue()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Mensaje a firmar");
            var (publicKey, privateKey) = _service.GenerateKeyPair(2048);

            // Act
            var signature = _service.Sign(data, privateKey);
            var isValid = _service.VerifySignature(data, signature, publicKey);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifySignature_WithModifiedData_ShouldReturnFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Mensaje a firmar");
            var modifiedData = Encoding.UTF8.GetBytes("Mensaje modificado");
            var (publicKey, privateKey) = _service.GenerateKeyPair(2048);

            // Act
            var signature = _service.Sign(data, privateKey);
            var isValid = _service.VerifySignature(modifiedData, signature, publicKey);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifySignature_WithWrongKey_ShouldReturnFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Mensaje a firmar");
            var (publicKey1, privateKey1) = _service.GenerateKeyPair(2048);
            var (publicKey2, _) = _service.GenerateKeyPair(2048);

            // Act
            var signature = _service.Sign(data, privateKey1);
            var isValid = _service.VerifySignature(data, signature, publicKey2);

            // Assert
            Assert.False(isValid);
        }
    }
}