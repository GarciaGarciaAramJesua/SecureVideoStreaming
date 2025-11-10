using System;
using System.Collections.Generic;

namespace SecureVideoStreaming.Models.Entities
{
    public class Video
    {
        public int IdVideo { get; set; }
        public int IdAdministrador { get; set; }
        public string TituloVideo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string NombreArchivoCifrado { get; set; } = string.Empty;
        public long TamañoArchivo { get; set; }
        public int? Duracion { get; set; }
        public string? FormatoVideo { get; set; }
        public string RutaAlmacenamiento { get; set; } = string.Empty;
        public string EstadoProcesamiento { get; set; } = "Procesando"; // 'Procesando', 'Disponible', 'Error', 'Eliminado'
        
        // Metadata
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
        
        // Relaciones
        public User Administrador { get; set; } = null!;
        public CryptoData? DatosCriptograficos { get; set; }
        public ICollection<Permission> Permisos { get; set; } = new List<Permission>();
        public ICollection<AccessLog> RegistrosAccesos { get; set; } = new List<AccessLog>();
    }
}