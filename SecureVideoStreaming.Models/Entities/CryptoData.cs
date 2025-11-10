using System;

namespace SecureVideoStreaming.Models.Entities
{
    /// <summary>
    /// Tabla DatosCriptograficosVideos: Datos criptográficos de cada video
    /// </summary>
    public class CryptoData
    {
        public int IdDatoCripto { get; set; }
        public int IdVideo { get; set; }
        
        // Claves y datos de cifrado
        public byte[] KEKCifrada { get; set; } = Array.Empty<byte>();
        public string AlgoritmoKEK { get; set; } = "ChaCha20-Poly1305";
        public byte[] Nonce { get; set; } = Array.Empty<byte>(); // 12 bytes para ChaCha20
        public byte[] AuthTag { get; set; } = Array.Empty<byte>(); // 16 bytes
        public byte[]? AAD { get; set; } // Additional Authenticated Data
        
        // Integridad y autenticación
        public byte[] HashSHA256Original { get; set; } = Array.Empty<byte>(); // 32 bytes
        public byte[] HMACDelVideo { get; set; } = Array.Empty<byte>(); // 64 bytes
        
        // Metadata
        public DateTime FechaGeneracionClaves { get; set; } = DateTime.UtcNow;
        public string VersionAlgoritmo { get; set; } = "1.0";
        
        // Relación
        public Video Video { get; set; } = null!;
    }
}
