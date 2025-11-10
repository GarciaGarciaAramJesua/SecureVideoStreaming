using System;

namespace SecureVideoStreaming.Models.Entities
{
    /// <summary>
    /// Tabla ClavesUsuarios: Gestión de claves criptográficas por usuario
    /// </summary>
    public class UserKeys
    {
        public int IdClaveUsuario { get; set; }
        public int IdUsuario { get; set; }
        public byte[]? ClaveHMAC { get; set; }
        public byte[] FingerprintClavePublica { get; set; } = Array.Empty<byte>();
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaExpiracion { get; set; }
        
        // Relación
        public User Usuario { get; set; } = null!;
    }
}
