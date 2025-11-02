using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVideoStreaming.Services.Cryptography.Implementations
{
    public class RsaService : IRsaService
    {
        public (string publicKey, string privateKey) GenerateKeyPair(int keySize = 2048)
        {
            if (keySize != 2048 && keySize != 4096)
                throw new ArgumentException("El tamaño de clave debe ser 2048 o 4096 bits", nameof(keySize));

            try
            {
                var keyPairGenerator = new RsaKeyPairGenerator();
                keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
                var keyPair = keyPairGenerator.GenerateKeyPair();

                // Exportar a formato PEM
                string publicKeyPem, privateKeyPem;

                using (var publicWriter = new StringWriter())
                {
                    var pemWriter = new PemWriter(publicWriter);
                    pemWriter.WriteObject(keyPair.Public);
                    publicKeyPem = publicWriter.ToString();
                }

                using (var privateWriter = new StringWriter())
                {
                    var pemWriter = new PemWriter(privateWriter);
                    pemWriter.WriteObject(keyPair.Private);
                    privateKeyPem = privateWriter.ToString();
                }

                return (publicKeyPem, privateKeyPem);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al generar par de claves RSA", ex);
            }
        }

        public byte[] Encrypt(byte[] data, string publicKeyPem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (string.IsNullOrWhiteSpace(publicKeyPem))
                throw new ArgumentException("La clave pública no puede estar vacía", nameof(publicKeyPem));

            try
            {
                // Cargar clave pública
                AsymmetricKeyParameter publicKey;
                using (var reader = new StringReader(publicKeyPem))
                {
                    var pemReader = new PemReader(reader);
                    publicKey = (AsymmetricKeyParameter)pemReader.ReadObject();
                }

                // Cifrar con OAEP (SHA-256)
                var engine = new OaepEncoding(new RsaEngine(), new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
                engine.Init(true, publicKey);

                return engine.ProcessBlock(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al cifrar con RSA", ex);
            }
        }

        public byte[] Decrypt(byte[] encryptedData, string privateKeyPem)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentException("Los datos cifrados no pueden estar vacíos", nameof(encryptedData));

            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new ArgumentException("La clave privada no puede estar vacía", nameof(privateKeyPem));

            try
            {
                // Cargar clave privada
                AsymmetricKeyParameter privateKey;
                using (var reader = new StringReader(privateKeyPem))
                {
                    var pemReader = new PemReader(reader);
                    var keyPair = pemReader.ReadObject();
                    
                    if (keyPair is AsymmetricCipherKeyPair pair)
                        privateKey = pair.Private;
                    else
                        privateKey = (AsymmetricKeyParameter)keyPair;
                }

                // Descifrar con OAEP (SHA-256)
                var engine = new OaepEncoding(new RsaEngine(), new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
                engine.Init(false, privateKey);

                return engine.ProcessBlock(encryptedData, 0, encryptedData.Length);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al descifrar con RSA", ex);
            }
        }

        public byte[] Sign(byte[] data, string privateKeyPem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new ArgumentException("La clave privada no puede estar vacía", nameof(privateKeyPem));

            try
            {
                // Cargar clave privada
                AsymmetricKeyParameter privateKey;
                using (var reader = new StringReader(privateKeyPem))
                {
                    var pemReader = new PemReader(reader);
                    var keyPair = pemReader.ReadObject();
                    
                    if (keyPair is AsymmetricCipherKeyPair pair)
                        privateKey = pair.Private;
                    else
                        privateKey = (AsymmetricKeyParameter)keyPair;
                }

                // Firmar con SHA-256
                var signer = SignerUtilities.GetSigner("SHA256withRSA");
                signer.Init(true, privateKey);
                signer.BlockUpdate(data, 0, data.Length);

                return signer.GenerateSignature();
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error al firmar con RSA", ex);
            }
        }

        public bool VerifySignature(byte[] data, byte[] signature, string publicKeyPem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Los datos no pueden estar vacíos", nameof(data));

            if (signature == null || signature.Length == 0)
                throw new ArgumentException("La firma no puede estar vacía", nameof(signature));

            if (string.IsNullOrWhiteSpace(publicKeyPem))
                throw new ArgumentException("La clave pública no puede estar vacía", nameof(publicKeyPem));

            try
            {
                // Cargar clave pública
                AsymmetricKeyParameter publicKey;
                using (var reader = new StringReader(publicKeyPem))
                {
                    var pemReader = new PemReader(reader);
                    publicKey = (AsymmetricKeyParameter)pemReader.ReadObject();
                }

                // Verificar firma con SHA-256
                var verifier = SignerUtilities.GetSigner("SHA256withRSA");
                verifier.Init(false, publicKey);
                verifier.BlockUpdate(data, 0, data.Length);

                return verifier.VerifySignature(signature);
            }
            catch
            {
                return false;
            }
        }
    }
}