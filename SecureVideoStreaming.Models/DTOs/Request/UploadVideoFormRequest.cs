using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    /// <summary>
    /// Request para subir video con form-data (compatible con Swagger)
    /// </summary>
    public class UploadVideoFormRequest
    {
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El archivo de video es requerido")]
        public IFormFile VideoFile { get; set; } = null!;
    }
}
