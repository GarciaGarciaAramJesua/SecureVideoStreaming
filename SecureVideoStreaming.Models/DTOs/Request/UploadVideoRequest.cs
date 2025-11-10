using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    public class UploadVideoRequest
    {
        [Required(ErrorMessage = "El nombre del archivo es requerido")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID del administrador es requerido")]
        public int IdAdministrador { get; set; }

        public string? Descripcion { get; set; }
    }
}
