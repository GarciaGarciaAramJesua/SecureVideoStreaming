namespace SecureVideoStreaming.Models.DTOs.Response
{
    /// <summary>
    /// Response para el grid de videos disponibles para usuarios
    /// </summary>
    public class VideoGridItemResponse
    {
        public int IdVideo { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public long TamañoArchivo { get; set; }
        public string TamañoArchivoFormateado { get; set; } = string.Empty;
        public string TamañoFormateado => TamañoArchivoFormateado; // Alias para la vista
        public int? Duracion { get; set; }
        public string? DuracionFormateada { get; set; }
        public string? FormatoVideo { get; set; }
        public DateTime FechaSubida { get; set; }
        public string NombreAdministrador { get; set; } = string.Empty;
        public string AlgoritmoCifrado { get; set; } = "ChaCha20-Poly1305"; // Por defecto
        
        // Información de permisos
        public bool TienePermiso { get; set; }
        public int? IdPermiso { get; set; }
        public string? TipoPermiso { get; set; }
        public DateTime? FechaOtorgamiento { get; set; }
        public DateTime? FechaPermiso => FechaOtorgamiento; // Alias para la vista
        public DateTime? FechaExpiracion { get; set; }
        public int NumeroAccesos { get; set; }
        public int ContadorAccesos => NumeroAccesos; // Alias para la vista
        public DateTime? UltimoAcceso { get; set; }
        
        // Estado visual
        public bool PermiteVisualizacion { get; set; }
        public string EstadoPermiso { get; set; } = string.Empty; // "Activo", "Expirado", "Sin Permiso"
    }
}
