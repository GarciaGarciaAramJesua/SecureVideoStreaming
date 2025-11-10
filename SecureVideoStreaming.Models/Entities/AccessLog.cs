using System;

namespace SecureVideoStreaming.Models.Entities
{
    /// <summary>
    /// Tabla RegistroAccesos: Auditoría de accesos a videos
    /// </summary>
    public class AccessLog
    {
        public long IdRegistro { get; set; }
        public int IdUsuario { get; set; }
        public int IdVideo { get; set; }
        public string TipoAcceso { get; set; } = string.Empty; // 'Visualizacion', 'Descarga', 'SolicitudClave', 'Verificacion'
        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }
        public string? DireccionIP { get; set; }
        public string? UserAgent { get; set; }
        public DateTime FechaHoraAcceso { get; set; } = DateTime.UtcNow;
        public int? DuracionAcceso { get; set; } // En segundos
        
        // Relaciones
        public User Usuario { get; set; } = null!;
        public Video Video { get; set; } = null!;
    }
}
