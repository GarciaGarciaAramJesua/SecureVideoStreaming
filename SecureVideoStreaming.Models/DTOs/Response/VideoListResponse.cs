namespace SecureVideoStreaming.Models.DTOs.Response
{
    public class VideoListResponse
    {
        public int IdVideo { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public long TamañoArchivo { get; set; }
        public int? Duracion { get; set; }
        public string? FormatoVideo { get; set; }
        public string EstadoProcesamiento { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; }
        public string NombreAdministrador { get; set; } = string.Empty;
    }
}
