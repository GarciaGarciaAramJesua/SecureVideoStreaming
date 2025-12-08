using SecureVideoStreaming.Models.DTOs.Response;

namespace SecureVideoStreaming.Services.Business.Interfaces
{
    /// <summary>
    /// Servicio para distribución segura de claves KEK a consumidores autorizados
    /// </summary>
    public interface IKeyDistributionService
    {
        /// <summary>
        /// Obtener paquete de claves cifrado para un consumidor autorizado
        /// </summary>
        /// <param name="videoId">ID del video</param>
        /// <param name="userId">ID del usuario solicitante</param>
        /// <param name="userPublicKey">Clave pública RSA del usuario (PEM format)</param>
        /// <returns>Paquete con KEK cifrada con la clave pública del usuario, nonce y authTag</returns>
        Task<ApiResponse<KeyPackageResponse>> GetKeyPackageAsync(int videoId, int userId, string userPublicKey);

        /// <summary>
        /// Validar y preparar token de streaming
        /// </summary>
        Task<ApiResponse<StreamingTokenResponse>> GenerateStreamingTokenAsync(int videoId, int userId);

        /// <summary>
        /// Verificar token de streaming
        /// </summary>
        Task<bool> ValidateStreamingTokenAsync(string token, int videoId, int userId);
    }
}
