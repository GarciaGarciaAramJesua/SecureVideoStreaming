using System;

namespace SecureVideoStreaming.Models.Entities
{
    public class Permission
    {
        public int IdPermiso { get; set; }
        public int IdVideo { get; set; }
        public int IdUsuario { get; set; }
        public string TipoPermiso { get; set; } = "Lectura"; // 'Pendiente', 'Aprobado', 'Revocado'
        
        // Metadata
        public DateTime FechaOtorgamiento { get; set; } = DateTime.UtcNow;
        public DateTime? FechaExpiracion { get; set; }
        public DateTime? FechaRevocacion { get; set; }
        public int NumeroAccesos { get; set; } = 0;
        public int? MaxAccesos { get; set; } // Límite de accesos (null = ilimitado)
        public DateTime? UltimoAcceso { get; set; }
        public string? Justificacion { get; set; } // Justificación de la solicitud de acceso
        
        // Otorgamiento y Revocación
        public int OtorgadoPor { get; set; }
        public int? RevocadoPor { get; set; }
        
        // Relaciones
        public Video Video { get; set; } = null!;
        public User Usuario { get; set; } = null!;
        public User UsuarioOtorgante { get; set; } = null!;
        public User? UsuarioRevocador { get; set; }
    }
}