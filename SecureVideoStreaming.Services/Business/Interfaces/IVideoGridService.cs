using SecureVideoStreaming.Models.DTOs.Response;

namespace SecureVideoStreaming.Services.Business.Interfaces
{
    /// <summary>
    /// Servicio para el Grid de videos (vista de usuarios)
    /// </summary>
    public interface IVideoGridService
    {
        /// <summary>
        /// Obtener grid de videos disponibles con información de permisos del usuario
        /// </summary>
        Task<ApiResponse<List<VideoGridItemResponse>>> GetVideoGridForUserAsync(int userId);

        /// <summary>
        /// Obtener grid de videos con filtros
        /// </summary>
        Task<ApiResponse<List<VideoGridItemResponse>>> GetVideoGridWithFiltersAsync(
            int userId, 
            string? searchTerm = null,
            string? administrador = null,
            bool? soloConPermiso = null);

        /// <summary>
        /// Obtener detalles de un video para el grid
        /// </summary>
        Task<ApiResponse<VideoGridItemResponse>> GetVideoGridItemAsync(int videoId, int userId);
    }
}
