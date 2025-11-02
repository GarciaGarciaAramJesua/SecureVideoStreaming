using SecureVideoStreaming.Models.Enums;
using System;
using System.Collections.Generic;

namespace SecureVideoStreaming.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        
        // Criptografía
        public string PublicKeyRsa { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // PBKDF2
        public string Salt { get; set; } = string.Empty;
        public string? HmacKey { get; set; } // Solo para Owners
        
        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Relaciones
        public ICollection<Video> OwnedVideos { get; set; } = new List<Video>();
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}