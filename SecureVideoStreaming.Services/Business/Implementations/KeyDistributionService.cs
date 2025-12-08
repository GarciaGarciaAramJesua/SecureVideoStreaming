using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using SecureVideoStreaming.Services.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class KeyDistributionService : IKeyDistributionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IRsaService _rsaService;
        private readonly IKeyManagementService _keyManagementService;

        public KeyDistributionService(
            ApplicationDbContext context,
            IPermissionService permissionService,
            IRsaService rsaService,
            IKeyManagementService keyManagementService)
        {
            _context = context;
            _permissionService = permissionService;
            _rsaService = rsaService;
            _keyManagementService = keyManagementService;
        }

        public async Task<ApiResponse<KeyPackageResponse>> GetKeyPackageAsync(int videoId, int userId, string userPublicKey)
        {
            try
            {
                // 1. Verificar si el usuario es el administrador dueño del video
                var video = await _context.Videos
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);
                
                if (video == null)
                {
                    return ApiResponse<KeyPackageResponse>.ErrorResponse("Video no encontrado");
                }

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == userId);

                bool isAdminOwner = usuario != null && 
                                   usuario.TipoUsuario == "Administrador" && 
                                   video.IdAdministrador == userId;

                if (!isAdminOwner)
                {
                    // Para usuarios no administradores o administradores que no son dueños, validar permisos
                    var hasAccessResponse = await _permissionService.HasAccessAsync(videoId, userId);
                    if (!hasAccessResponse.Success || !hasAccessResponse.Data)
                    {
                        await _permissionService.RegisterAccessAsync(videoId, userId, false, "Permiso denegado o inexistente");
                        return ApiResponse<KeyPackageResponse>.ErrorResponse(
                            hasAccessResponse.Message ?? "No tiene permiso para acceder a este video");
                    }
                }

                // 2. Obtener datos criptográficos del video
                var cryptoData = await _context.DatosCriptograficosVideos
                    .FirstOrDefaultAsync(c => c.IdVideo == videoId)
                    ?? throw new KeyNotFoundException($"Datos criptográficos del video {videoId} no encontrados");

                // 3. Descifrar KEK usando la clave privada del servidor
                var serverPrivateKey = _keyManagementService.GetServerPrivateKey();
                var kekBytes = _rsaService.Decrypt(cryptoData.KEKCifrada, serverPrivateKey);

                // 4. Re-cifrar KEK con la clave pública del consumidor
                var rekencryptedKek = _rsaService.Encrypt(kekBytes, userPublicKey);

                // 5. Generar token de streaming
                var streamingToken = await GenerateStreamingTokenAsync(videoId, userId);

                // 6. Incrementar contador de accesos
                var permission = await _context.Permisos
                    .FirstOrDefaultAsync(p => p.IdVideo == videoId && p.IdUsuario == userId && p.TipoPermiso == "Aprobado");
                
                if (permission != null)
                {
                    await _permissionService.IncrementAccessCountAsync(permission.IdPermiso);
                }

                // 7. Registrar acceso exitoso
                await _permissionService.RegisterAccessAsync(videoId, userId, true);

                // 8. Crear respuesta
                var response = new KeyPackageResponse
                {
                    EncryptedKekForUser = Convert.ToBase64String(rekencryptedKek),
                    Nonce = Convert.ToBase64String(cryptoData.Nonce),
                    AuthTag = Convert.ToBase64String(cryptoData.AuthTag),
                    Algorithm = "ChaCha20-Poly1305",
                    VideoId = videoId,
                    StreamingToken = streamingToken.Data?.Token ?? string.Empty,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };

                return ApiResponse<KeyPackageResponse>.SuccessResponse(
                    response, 
                    "Paquete de claves generado exitosamente");
            }
            catch (Exception ex) when (ex is not VideoNotFoundException && ex is not UserNotFoundException)
            {
                await _permissionService.RegisterAccessAsync(videoId, userId, false, ex.Message);
                throw;
            }
        }

        public async Task<ApiResponse<StreamingTokenResponse>> GenerateStreamingTokenAsync(int videoId, int userId)
        {
            // Obtener información del video
            var video = await _context.Videos
                .FirstOrDefaultAsync(v => v.IdVideo == videoId)
                ?? throw new VideoNotFoundException($"Video con ID {videoId} no encontrado");

            // Generar token JWT-like con HMAC-SHA256
            var tokenData = $"{videoId}|{userId}|{DateTime.UtcNow.AddHours(1):O}";
            var key = _keyManagementService.GetServerPrivateKey();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key.Substring(0, Math.Min(64, key.Length))));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(tokenData));
            var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData))}.{Convert.ToBase64String(hash)}";

            var response = new StreamingTokenResponse
            {
                Token = token,
                VideoId = videoId,
                StreamingUrl = $"/api/streaming/video/{videoId}",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                FileSizeBytes = video.TamañoArchivo,
                ContentType = "application/octet-stream"
            };

            return ApiResponse<StreamingTokenResponse>.SuccessResponse(response, "Token generado exitosamente");
        }

        public async Task<bool> ValidateStreamingTokenAsync(string token, int videoId, int userId)
        {
            try
            {
                // Separar token en datos y firma
                var parts = token.Split('.');
                if (parts.Length != 2)
                    return false;

                var tokenDataBase64 = parts[0];
                var signatureBase64 = parts[1];

                // Decodificar datos
                var tokenData = Encoding.UTF8.GetString(Convert.FromBase64String(tokenDataBase64));
                var tokenParts = tokenData.Split('|');
                
                if (tokenParts.Length != 3)
                    return false;

                var tokenVideoId = int.Parse(tokenParts[0]);
                var tokenUserId = int.Parse(tokenParts[1]);
                var expiresAt = DateTime.Parse(tokenParts[2]);

                // Validar contenido
                if (tokenVideoId != videoId || tokenUserId != userId)
                    return false;

                if (expiresAt < DateTime.UtcNow)
                    return false;

                // Validar firma
                var key = _keyManagementService.GetServerPrivateKey();
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key.Substring(0, Math.Min(64, key.Length))));
                var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(tokenData));
                var actualHash = Convert.FromBase64String(signatureBase64);

                if (!expectedHash.SequenceEqual(actualHash))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
