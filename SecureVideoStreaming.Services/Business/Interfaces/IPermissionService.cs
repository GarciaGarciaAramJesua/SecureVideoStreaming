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
    }
}
