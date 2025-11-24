using SecureVideoStreaming.Models.DTOs.Response;

namespace SecureVideoStreaming.Services.Business.Interfaces
{
    /// <summary>
    /// Servicio para distribución segura de claves de cifrado
    /// </summary>
    public interface IKeyDistributionService
    {
        /// <summary>
        /// Distribuir claves de cifrado a un usuario con permiso
        /// Las claves son re-cifradas con la clave pública RSA del usuario
        /// </summary>
        Task<ApiResponse<KeyDistributionResponse>> DistributeKeysAsync(int videoId, int userId);

        /// <summary>
        /// Validar que un usuario puede recibir las claves de un video
        /// </summary>
        Task<ApiResponse<bool>> ValidateKeyDistributionAsync(int videoId, int userId);

        /// <summary>
        /// Registrar la distribución de claves en el log de auditoría
        /// </summary>
        Task<ApiResponse<bool>> LogKeyDistributionAsync(int videoId, int userId, bool exitoso, string? mensajeError = null);
    }
}
