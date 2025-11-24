using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD
using Microsoft.Extensions.Configuration;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;
=======
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using SecureVideoStreaming.Services.Exceptions;
using System.Security.Cryptography;
using System.Text;
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class KeyDistributionService : IKeyDistributionService
    {
        private readonly ApplicationDbContext _context;
<<<<<<< HEAD
        private readonly IRsaService _rsaService;
        private readonly IConfiguration _configuration;
        private readonly IPermissionService _permissionService;
        private readonly string _serverPrivateKeyPath;

        public KeyDistributionService(
            ApplicationDbContext context,
            IRsaService rsaService,
            IConfiguration configuration,
            IPermissionService permissionService)
        {
            _context = context;
            _rsaService = rsaService;
            _configuration = configuration;
            _permissionService = permissionService;
            
            var keysPath = configuration["Storage:KeysPath"] ?? "./Storage/Keys";
            _serverPrivateKeyPath = Path.Combine(keysPath, "server_private_key.pem");
            
            // Asegurar que el directorio existe
            if (!Directory.Exists(keysPath))
            {
                Directory.CreateDirectory(keysPath);
            }
        }

        public async Task<ApiResponse<KeyDistributionResponse>> DistributeKeysAsync(int videoId, int userId)
        {
            try
            {
                // 1. Validar que el usuario tiene permiso
                var validationResult = await ValidateKeyDistributionAsync(videoId, userId);
                if (!validationResult.Success || !validationResult.Data)
                {
                    await LogKeyDistributionAsync(videoId, userId, false, "Usuario sin permiso válido");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("No tiene permiso para acceder a este video");
                }

                // 2. Obtener datos del video y criptografía
                var video = await _context.Videos
                    .Include(v => v.DatosCriptograficos)
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    await LogKeyDistributionAsync(videoId, userId, false, "Video no encontrado");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("Video no encontrado");
                }

                if (video.DatosCriptograficos == null)
                {
                    await LogKeyDistributionAsync(videoId, userId, false, "Datos criptográficos no encontrados");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("Datos criptográficos no disponibles");
                }

                // 3. Obtener clave pública del usuario
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario == null || string.IsNullOrEmpty(usuario.ClavePublicaRSA))
                {
                    await LogKeyDistributionAsync(videoId, userId, false, "Usuario sin clave pública RSA");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("Usuario sin clave pública configurada");
                }

                // 4. Descifrar KEK con clave privada del servidor
                byte[] kek;
                try
                {
                    var serverPrivateKey = await GetOrCreateServerPrivateKeyAsync();
                    kek = _rsaService.Decrypt(video.DatosCriptograficos.KEKCifrada, serverPrivateKey);
                }
                catch (CryptographicException ex)
                {
                    await LogKeyDistributionAsync(videoId, userId, false, $"Error al descifrar KEK: {ex.Message}");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("Error al procesar claves del servidor");
                }

                // 5. Re-cifrar KEK con clave pública del usuario
                byte[] kekCifradaParaUsuario;
                try
                {
                    kekCifradaParaUsuario = _rsaService.Encrypt(kek, usuario.ClavePublicaRSA);
                }
                catch (CryptographicException ex)
                {
                    await LogKeyDistributionAsync(videoId, userId, false, $"Error al cifrar KEK para usuario: {ex.Message}");
                    return ApiResponse<KeyDistributionResponse>.ErrorResponse("Error al cifrar claves para el usuario");
                }

                // 6. Obtener permiso para actualizar contador
                var permiso = await _context.Permisos
                    .FirstOrDefaultAsync(p => p.IdVideo == videoId 
                        && p.IdUsuario == userId 
                        && p.FechaRevocacion == null);

                if (permiso != null)
                {
                    await _permissionService.IncrementAccessCountAsync(permiso.IdPermiso);
                }

                // 7. Crear respuesta con todas las claves y datos
                var response = new KeyDistributionResponse
                {
                    IdVideo = video.IdVideo,
                    TituloVideo = video.TituloVideo,
                    KEKCifradaParaUsuario = Convert.ToBase64String(kekCifradaParaUsuario),
                    Nonce = Convert.ToBase64String(video.DatosCriptograficos.Nonce),
                    AuthTag = Convert.ToBase64String(video.DatosCriptograficos.AuthTag),
                    AlgoritmoCifrado = video.DatosCriptograficos.AlgoritmoKEK,
                    HashOriginal = Convert.ToBase64String(video.DatosCriptograficos.HashSHA256Original),
                    HMAC = Convert.ToBase64String(video.DatosCriptograficos.HMACDelVideo),
                    VideoDownloadUrl = $"/api/videos/{videoId}/download",
                    TamañoArchivo = video.TamañoArchivo,
                    FechaGeneracion = DateTime.UtcNow,
                    IdPermiso = permiso?.IdPermiso ?? 0
                };

                // 8. Registrar distribución exitosa
                await LogKeyDistributionAsync(videoId, userId, true);

                return ApiResponse<KeyDistributionResponse>.SuccessResponse(
                    response, 
                    "Claves distribuidas exitosamente");
            }
            catch (Exception ex)
            {
                await LogKeyDistributionAsync(videoId, userId, false, $"Error inesperado: {ex.Message}");
                return ApiResponse<KeyDistributionResponse>.ErrorResponse($"Error al distribuir claves: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ValidateKeyDistributionAsync(int videoId, int userId)
        {
            try
            {
                // 1. Verificar que el video existe y está disponible
                var video = await _context.Videos.FindAsync(videoId);
                if (video == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Video no encontrado");
                }

                if (video.EstadoProcesamiento != "Disponible")
                {
                    return ApiResponse<bool>.ErrorResponse("Video no disponible");
                }

                // 2. Verificar permiso activo
                var checkPermission = await _permissionService.CheckPermissionAsync(videoId, userId);
                if (!checkPermission.Success || !checkPermission.Data)
                {
                    return ApiResponse<bool>.ErrorResponse("Usuario sin permiso válido");
                }

                // 3. Verificar que el usuario existe y está activo
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario == null || !usuario.Activo)
                {
                    return ApiResponse<bool>.ErrorResponse("Usuario no disponible");
                }

                return ApiResponse<bool>.SuccessResponse(true, "Validación exitosa");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error en validación: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> LogKeyDistributionAsync(int videoId, int userId, bool exitoso, string? mensajeError = null)
        {
            try
            {
                var accessLog = new AccessLog
                {
                    IdVideo = videoId,
                    IdUsuario = userId,
                    TipoAcceso = "SolicitudClave",
                    Exitoso = exitoso,
                    MensajeError = mensajeError,
                    FechaHoraAcceso = DateTime.UtcNow
                };

                _context.RegistroAccesos.Add(accessLog);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                // No fallar si el log falla
                Console.WriteLine($"Error al registrar distribución de claves: {ex.Message}");
                return ApiResponse<bool>.ErrorResponse($"Error al registrar: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener o crear la clave privada RSA del servidor
        /// Esta clave es persistente y se usa para descifrar las KEKs de los videos
        /// </summary>
        private async Task<string> GetOrCreateServerPrivateKeyAsync()
        {
            // Si la clave ya existe, leerla
            if (File.Exists(_serverPrivateKeyPath))
            {
                return await File.ReadAllTextAsync(_serverPrivateKeyPath);
            }

            // Si no existe, generar nueva y guardarla
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);
            
            await File.WriteAllTextAsync(_serverPrivateKeyPath, privateKey);
            
            // También guardar la clave pública para referencia
            var publicKeyPath = Path.Combine(
                Path.GetDirectoryName(_serverPrivateKeyPath)!, 
                "server_public_key.pem");
            await File.WriteAllTextAsync(publicKeyPath, publicKey);

            return privateKey;
        }

        /// <summary>
        /// Obtener la clave pública del servidor
        /// </summary>
        public async Task<string> GetServerPublicKeyAsync()
        {
            var publicKeyPath = Path.Combine(
                Path.GetDirectoryName(_serverPrivateKeyPath)!,
                "server_public_key.pem");

            if (File.Exists(publicKeyPath))
            {
                return await File.ReadAllTextAsync(publicKeyPath);
            }

            // Si no existe, generar las claves
            await GetOrCreateServerPrivateKeyAsync();
            return await File.ReadAllTextAsync(publicKeyPath);
        }
=======
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
                // 1. Validar que el usuario tiene permiso activo
                var hasAccessResponse = await _permissionService.HasAccessAsync(videoId, userId);
                if (!hasAccessResponse.Success || !hasAccessResponse.Data)
                {
                    await _permissionService.RegisterAccessAsync(videoId, userId, false, "Permiso denegado o inexistente");
                    return ApiResponse<KeyPackageResponse>.ErrorResponse(
                        hasAccessResponse.Message ?? "No tiene permiso para acceder a este video");
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
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
    }
}
