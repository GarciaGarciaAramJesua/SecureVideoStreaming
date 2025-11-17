namespace SecureVideoStreaming.Models.DTOs.Response
{
    public class VideoIntegrityResponse
    {
        public int IdVideo { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string HashSHA256Original { get; set; } = string.Empty;
        public DateTime FechaVerificacion { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
