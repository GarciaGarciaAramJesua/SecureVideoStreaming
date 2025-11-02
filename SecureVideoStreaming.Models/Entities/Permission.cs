using System;

namespace SecureVideoStreaming.Models.Entities
{
    public class Permission
    {
        public Guid Id { get; set; }
        
        // Relaciones
        public Guid VideoId { get; set; }
        public Video Video { get; set; } = null!;
        
        public Guid ConsumerId { get; set; }
        public User Consumer { get; set; } = null!;
        
        // Control de acceso
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}