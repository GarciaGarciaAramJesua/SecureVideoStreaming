using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Text;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CryptoTestController : ControllerBase
    {
        private readonly IChaCha20Poly1305Service _chaChaService;
        private readonly IRsaService _rsaService;
        private readonly IHashService _hashService;
        private readonly IHmacService _hmacService;

        public CryptoTestController(
            IChaCha20Poly1305Service chaChaService,
            IRsaService rsaService,
            IHashService hashService,
            IHmacService hmacService)
        {
            _chaChaService = chaChaService;
            _rsaService = rsaService;
            _hashService = hashService;
            _hmacService = hmacService;
        }

        [HttpGet("test-chacha20")]
        public IActionResult TestChaCha20()
        {
            var plaintext = "Hola, este es un mensaje de prueba con ChaCha20-Poly1305";
            var data = Encoding.UTF8.GetBytes(plaintext);
            var key = _chaChaService.GenerateKey();

            var (ciphertext, nonce, authTag) = _chaChaService.Encrypt(data, key);
            var decrypted = _chaChaService.Decrypt(ciphertext, key, nonce, authTag);
            var decryptedText = Encoding.UTF8.GetString(decrypted);

            return Ok(new
            {
                algorithm = "ChaCha20-Poly1305",
                original = plaintext,
                decrypted = decryptedText,
                success = plaintext == decryptedText,
                ciphertextLength = ciphertext.Length,
                nonceLength = nonce.Length,
                authTagLength = authTag.Length
            });
        }

        [HttpGet("test-rsa")]
        public IActionResult TestRsa()
        {
            var message = "Mensaje secreto";
            var data = Encoding.UTF8.GetBytes(message);
            
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);
            var encrypted = _rsaService.Encrypt(data, publicKey);
            var decrypted = _rsaService.Decrypt(encrypted, privateKey);
            var decryptedMessage = Encoding.UTF8.GetString(decrypted);

            var signature = _rsaService.Sign(data, privateKey);
            var isValid = _rsaService.VerifySignature(data, signature, publicKey);

            return Ok(new
            {
                algorithm = "RSA-2048-OAEP",
                original = message,
                decrypted = decryptedMessage,
                encryptionSuccess = message == decryptedMessage,
                signatureValid = isValid,
                encryptedLength = encrypted.Length,
                signatureLength = signature.Length
            });
        }

        [HttpGet("test-hash")]
        public IActionResult TestHash()
        {
            var data = Encoding.UTF8.GetBytes("Datos para hashear");
            var hash = _hashService.ComputeSha256(data);
            var hashHex = _hashService.ComputeSha256Hex(data);

            var password = "MiContraseñaSegura123!";
            var salt = _hashService.GenerateSalt();
            var derivedKey = _hashService.DeriveKey(password, salt, 100000, 32);

            return Ok(new
            {
                algorithm = "SHA-256 & PBKDF2",
                hashLength = hash.Length,
hashHex = hashHex,
saltLength = salt.Length,
derivedKeyLength = derivedKey.Length,
derivedKeyHex = Convert.ToHexString(derivedKey).ToLowerInvariant()
});
}
    [HttpGet("test-hmac")]
    public IActionResult TestHmac()
    {
        var data = Encoding.UTF8.GetBytes("Datos para autenticar");
        var key = _hmacService.GenerateKey();
        
        var hmac = _hmacService.ComputeHmac(data, key);
        var hmacHex = _hmacService.ComputeHmacHex(data, key);
        var isValid = _hmacService.VerifyHmac(data, key, hmac);

        return Ok(new
        {
            algorithm = "HMAC-SHA256",
            hmacLength = hmac.Length,
            hmacHex = hmacHex,
            verificationSuccess = isValid,
            keyLength = key.Length
        });
    }

    [HttpGet("test-all")]
    public IActionResult TestAll()
    {
        try
        {
            // Test ChaCha20
            var plaintext = "Mensaje de prueba completo";
            var data = Encoding.UTF8.GetBytes(plaintext);
            var chachaKey = _chaChaService.GenerateKey();
            var (ciphertext, nonce, authTag) = _chaChaService.Encrypt(data, chachaKey);
            var decrypted = _chaChaService.Decrypt(ciphertext, chachaKey, nonce, authTag);
            var chachaSuccess = Encoding.UTF8.GetString(decrypted) == plaintext;

            // Test RSA
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);
            var rsaData = Encoding.UTF8.GetBytes("RSA Test");
            var encrypted = _rsaService.Encrypt(rsaData, publicKey);
            var rsaDecrypted = _rsaService.Decrypt(encrypted, privateKey);
            var rsaSuccess = Encoding.UTF8.GetString(rsaDecrypted) == "RSA Test";

            // Test Hash
            var hash = _hashService.ComputeSha256(data);
            var hashSuccess = hash.Length == 32;

            // Test HMAC
            var hmacKey = _hmacService.GenerateKey();
            var hmac = _hmacService.ComputeHmac(data, hmacKey);
            var hmacSuccess = _hmacService.VerifyHmac(data, hmacKey, hmac);

            return Ok(new
            {
                message = "Todas las pruebas criptográficas completadas",
                results = new
                {
                    chaCha20Poly1305 = new { success = chachaSuccess, status = "✓" },
                    rsa = new { success = rsaSuccess, status = "✓" },
                    sha256 = new { success = hashSuccess, status = "✓" },
                    hmac = new { success = hmacSuccess, status = "✓" },
                },
                allTestsPassed = chachaSuccess && rsaSuccess && hashSuccess && hmacSuccess
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Error en las pruebas criptográficas",
                message = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}
}