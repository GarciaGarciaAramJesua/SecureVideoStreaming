using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    /// <summary>
    /// Request para otorgar permiso de acceso a un video
    /// </summary>
    public class GrantPermissionRequest
    {
        [Required(ErrorMessage = "El ID del video es requerido")]
        public int IdVideo { get; set; }

        [Required(ErrorMessage = "El ID del usuario consumidor es requerido")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El ID del administrador otorgante es requerido")]
        public int OtorgadoPor { get; set; }

        /// <summary>
        /// Tipo de permiso: "Lectura" (permanente), "Temporal" (con expiración)
        /// </summary>
        [Required(ErrorMessage = "El tipo de permiso es requerido")]
        [RegularExpression("^(Lectura|Temporal)$", ErrorMessage = "Tipo de permiso inválido. Use 'Lectura' o 'Temporal'")]
        public string TipoPermiso { get; set; } = "Lectura";

        /// <summary>
        /// Fecha de expiración (requerida si TipoPermiso es "Temporal")
        /// </summary>
        public DateTime? FechaExpiracion { get; set; }
    }
}
