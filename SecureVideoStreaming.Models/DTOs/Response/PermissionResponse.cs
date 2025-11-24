namespace SecureVideoStreaming.Models.DTOs.Response
{
    /// <summary>
    /// Response con información de un permiso de acceso
    /// </summary>
    public class PermissionResponse
    {
        public int IdPermiso { get; set; }
        public int IdVideo { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
        public string TipoPermiso { get; set; } = string.Empty;
        public DateTime FechaOtorgamiento { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public DateTime? FechaRevocacion { get; set; }
        public int NumeroAccesos { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int OtorgadoPor { get; set; }
        public string NombreOtorgante { get; set; } = string.Empty;
        public int? RevocadoPor { get; set; }
        public string? NombreRevocador { get; set; }
        public bool EstaActivo { get; set; }
        public bool EstaExpirado { get; set; }
    }
}
