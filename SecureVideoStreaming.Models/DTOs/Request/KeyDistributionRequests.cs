using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    /// <summary>
    /// Request para solicitar acceso a un video
    /// </summary>
    public class AccessRequestDto
    {
        [Required]
        public int VideoId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "La justificación no puede exceder 500 caracteres")]
        public string Justificacion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para aprobar solicitud de acceso
    /// </summary>
    public class ApproveAccessRequest
    {
        public int? MaxAccesos { get; set; }
        public DateTime? FechaExpiracion { get; set; }
    }

    /// <summary>
    /// Request para obtener paquete de claves
    /// </summary>
    public class KeyPackageRequest
    {
        [Required]
        public int VideoId { get; set; }

        [Required]
        [StringLength(10000, ErrorMessage = "La clave pública es demasiado grande")]
        public string UserPublicKey { get; set; } = string.Empty;
    }
}
