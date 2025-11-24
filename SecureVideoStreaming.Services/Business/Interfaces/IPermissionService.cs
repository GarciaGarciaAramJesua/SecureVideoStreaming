using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;

namespace SecureVideoStreaming.Services.Business.Interfaces
{
    /// <summary>
    /// Servicio para gestión de permisos de acceso a videos
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
<<<<<<< HEAD
        /// Otorgar permiso a un usuario para acceder a un video
        /// </summary>
        Task<ApiResponse<PermissionResponse>> GrantPermissionAsync(GrantPermissionRequest request);

        /// <summary>
        /// Revocar permiso de acceso a un video
        /// </summary>
        Task<ApiResponse<bool>> RevokePermissionAsync(int permissionId, int adminId);

        /// <summary>
        /// Verificar si un usuario tiene permiso activo para un video
        /// </summary>
        Task<ApiResponse<bool>> CheckPermissionAsync(int videoId, int userId);

        /// <summary>
        /// Obtener todos los permisos de un video específico
        /// </summary>
        Task<ApiResponse<List<PermissionResponse>>> GetPermissionsByVideoAsync(int videoId, int adminId);

        /// <summary>
        /// Obtener todos los permisos de un usuario (videos a los que tiene acceso)
        /// </summary>
        Task<ApiResponse<List<PermissionResponse>>> GetPermissionsByUserAsync(int userId);

        /// <summary>
        /// Extender la fecha de expiración de un permiso
        /// </summary>
        Task<ApiResponse<PermissionResponse>> ExtendPermissionAsync(int permissionId, DateTime newExpiration, int adminId);

        /// <summary>
        /// Obtener detalles de un permiso específico
        /// </summary>
        Task<ApiResponse<PermissionResponse>> GetPermissionByIdAsync(int permissionId);

        /// <summary>
        /// Actualizar el contador de accesos de un permiso
        /// </summary>
        Task<ApiResponse<bool>> IncrementAccessCountAsync(int permissionId);
=======
        /// Solicitar acceso a un video
        /// </summary>
        Task<ApiResponse<PermissionResponse>> RequestAccessAsync(int videoId, int userId, string justificacion);

        /// <summary>
        /// Verificar si un usuario tiene acceso válido a un video
        /// </summary>
        Task<ApiResponse<bool>> HasAccessAsync(int videoId, int userId);

        /// <summary>
        /// Obtener detalles del permiso de un usuario para un video
        /// </summary>
        Task<ApiResponse<PermissionResponse>> GetPermissionAsync(int videoId, int userId);

        /// <summary>
        /// Aprobar solicitud de acceso (solo administrador dueño)
        /// </summary>
        Task<ApiResponse<PermissionResponse>> ApproveAccessAsync(int permisoId, int adminId, int? maxAccesos = null, DateTime? fechaExpiracion = null);

        /// <summary>
        /// Revocar acceso a un video (solo administrador dueño)
        /// </summary>
        Task<ApiResponse<bool>> RevokeAccessAsync(int permisoId, int adminId);

        /// <summary>
        /// Listar permisos de un video (solo administrador dueño)
        /// </summary>
        Task<ApiResponse<List<PermissionResponse>>> GetVideoPermissionsAsync(int videoId, int adminId);

        /// <summary>
        /// Listar mis permisos de acceso
        /// </summary>
        Task<ApiResponse<List<PermissionResponse>>> GetMyPermissionsAsync(int userId);

        /// <summary>
        /// Registrar acceso a video
        /// </summary>
        Task RegisterAccessAsync(int videoId, int userId, bool exitoso, string? mensajeError = null);

        /// <summary>
        /// Incrementar contador de accesos
        /// </summary>
        Task IncrementAccessCountAsync(int permisoId);
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
    }
}
