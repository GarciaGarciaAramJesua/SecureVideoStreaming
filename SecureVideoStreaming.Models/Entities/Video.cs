using System;
using System.Collections.Generic;

namespace SecureVideoStreaming.Models.Entities
{
    public class Video
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int DurationSeconds { get; set; }
        
        // Criptografía
        public string EncryptedFilePath { get; set; } = string.Empty;
        public string EncryptedKek { get; set; } = string.Empty; // KEK cifrada con RSA del servidor
        public string Nonce { get; set; } = string.Empty; // Nonce de 12 bytes para ChaCha20-Poly1305
        public string AuthTag { get; set; } = string.Empty; // Tag de autenticación de Poly1305
        public string Hmac { get; set; } = string.Empty; // HMAC del owner
        public string OriginalHash { get; set; } = string.Empty; // SHA-256 del video original
        
        // Metadata
        public DateTime UploadedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Relaciones
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}