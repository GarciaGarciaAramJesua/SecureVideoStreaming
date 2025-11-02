using SecureVideoStreaming.Services.Cryptography.Implementations;
using System.Text;
using Xunit;

namespace SecureVideoStreaming.Tests.Cryptography
{
    public class ChaCha20Poly1305ServiceTests
    {
        private readonly ChaCha20Poly1305Service _service;

        public ChaCha20Poly1305ServiceTests()
        {
            _service = new ChaCha20Poly1305Service();
        }

        [Fact]
        public void GenerateKey_ShouldReturn32Bytes()
        {
            // Act
            var key = _service.GenerateKey();

            // Assert
            Assert.NotNull(key);
            Assert.Equal(32, key.Length);
        }

        [Fact]
        public void GenerateNonce_ShouldReturn12Bytes()
        {
            // Act
            var nonce = _service.GenerateNonce();

            // Assert
            Assert.NotNull(nonce);
            Assert.Equal(12, nonce.Length);
        }

        [Fact]
        public void Encrypt_ShouldReturnValidCiphertextAndTag()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Hola, este es un mensaje de prueba");
            var key = _service.GenerateKey();

            // Act
            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key);

            // Assert
            Assert.NotNull(ciphertext);
            Assert.NotNull(nonce);
            Assert.NotNull(authTag);
            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(12, nonce.Length);
            Assert.Equal(16, authTag.Length);
            Assert.NotEqual(plaintext, ciphertext);
        }

        [Fact]
        public void Decrypt_ShouldReturnOriginalPlaintext()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Hola, este es un mensaje de prueba");
            var key = _service.GenerateKey();
            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key);

            // Act
            var decrypted = _service.Decrypt(ciphertext, key, nonce, authTag);

            // Assert
            Assert.Equal(plaintext, decrypted);
            Assert.Equal("Hola, este es un mensaje de prueba", Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void Decrypt_WithModifiedCiphertext_ShouldThrowException()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Hola, este es un mensaje de prueba");
            var key = _service.GenerateKey();
            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key);

            // Modificar el ciphertext
            ciphertext[0] ^= 0xFF;

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            {
                _service.Decrypt(ciphertext, key, nonce, authTag);
            });
        }

        [Fact]
        public void Decrypt_WithWrongKey_ShouldThrowException()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Hola, este es un mensaje de prueba");
            var key = _service.GenerateKey();
            var wrongKey = _service.GenerateKey();
            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key);

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            {
                _service.Decrypt(ciphertext, wrongKey, nonce, authTag);
            });
        }

        [Fact]
        public void Encrypt_WithAAD_ShouldAuthenticateAdditionalData()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Mensaje secreto");
            var aad = Encoding.UTF8.GetBytes("Metadata pública");
            var key = _service.GenerateKey();

            // Act
            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key, null, aad);
            var decrypted = _service.Decrypt(ciphertext, key, nonce, authTag, aad);

            // Assert
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void Decrypt_WithModifiedAAD_ShouldThrowException()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Mensaje secreto");
            var aad = Encoding.UTF8.GetBytes("Metadata pública");
            var modifiedAad = Encoding.UTF8.GetBytes("Metadata modificada");
            var key = _service.GenerateKey();

            var (ciphertext, nonce, authTag) = _service.Encrypt(plaintext, key, null, aad);

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            {
                _service.Decrypt(ciphertext, key, nonce, authTag, modifiedAad);
            });
        }

        [Fact]
        public void Encrypt_WithLargeData_ShouldWork()
        {
            // Arrange
            var largeData = new byte[1024 * 1024]; // 1 MB
            new Random().NextBytes(largeData);
            var key = _service.GenerateKey();

            // Act
            var (ciphertext, nonce, authTag) = _service.Encrypt(largeData, key);
            var decrypted = _service.Decrypt(ciphertext, key, nonce, authTag);

            // Assert
            Assert.Equal(largeData, decrypted);
        }
    }
}