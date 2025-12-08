using SecureVideoStreaming.Models.Enums;
using System;
using System.Collections.Generic;

namespace SecureVideoStreaming.Models.Entities
{
    public class User
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty; // 'Administrador' o 'Usuario'
        
        // Criptografía
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public string ClavePublicaRSA { get; set; } = string.Empty;
        public string? PublicKeyFingerprint { get; set; }
        
        // Metadata
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoAcceso { get; set; }
        public bool Activo { get; set; } = true;
        
        // Relaciones
        public ICollection<UserKeys> ClavesUsuarios { get; set; } = new List<UserKeys>();
        public ICollection<Video> VideosAdministrados { get; set; } = new List<Video>();
        public ICollection<Permission> Permisos { get; set; } = new List<Permission>();
        public ICollection<AccessLog> RegistrosAccesos { get; set; } = new List<AccessLog>();
    }
}