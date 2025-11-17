using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    public class UpdateVideoMetadataRequest
    {
        [StringLength(200)]
        public string? TituloVideo { get; set; }

        [StringLength(1000)]
        public string? Descripcion { get; set; }
    }
}
