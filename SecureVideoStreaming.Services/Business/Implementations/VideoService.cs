using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Text;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class VideoService : IVideoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVideoEncryptionService _videoEncryptionService;
        private readonly IKeyManagementService _keyManagementService;
        private readonly IConfiguration _configuration;
        private readonly string _videosPath;
        private readonly string _keysPath;
        private readonly string _serverPublicKeyPath;

        public VideoService(
            ApplicationDbContext context,
            IVideoEncryptionService videoEncryptionService,
            IKeyManagementService keyManagementService,
            IConfiguration configuration)
        {
            _context = context;
            _videoEncryptionService = videoEncryptionService;
            _keyManagementService = keyManagementService;
            _configuration = configuration;
            _videosPath = configuration["Storage:VideosPath"] ?? "./Storage/Videos";
            _keysPath = configuration["Storage:KeysPath"] ?? "./Storage/Keys";
            _serverPublicKeyPath = Path.Combine(_keysPath, "server_public_key.pem");

            // Crear directorios si no existen
            if (!Directory.Exists(_videosPath))
            {
                Directory.CreateDirectory(_videosPath);
            }
            if (!Directory.Exists(_keysPath))
            {
                Directory.CreateDirectory(_keysPath);
            }
        }

        public async Task<ApiResponse<VideoResponse>> UploadVideoAsync(UploadVideoRequest request, Stream videoStream)
        {
            try
            {
                // 1. Verificar que el usuario es administrador
                var admin = await _context.Usuarios.FindAsync(request.IdAdministrador);
                if (admin == null || admin.TipoUsuario != "Administrador")
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("Solo los administradores pueden subir videos");
                }

                // 2. Obtener clave HMAC del administrador
                var userKeys = await _context.ClavesUsuarios
                    .FirstOrDefaultAsync(k => k.IdUsuario == request.IdAdministrador);

                if (userKeys?.ClaveHMAC == null)
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("El administrador no tiene clave HMAC configurada");
                }

                // 3. Obtener clave pública del servidor
                var serverPublicKey = _keyManagementService.GetServerPublicKey();

<<<<<<< HEAD
                // 8. Cifrar KEK con clave pública del servidor (persistente)
                var serverPublicKey = await GetOrCreateServerPublicKeyAsync();
                var encryptedKek = _rsaService.Encrypt(kek, serverPublicKey);
=======
                // 4. Guardar video original temporalmente
                var tempOriginalPath = Path.Combine(_videosPath, $"temp_{Guid.NewGuid()}.tmp");
                await using (var fileStream = new FileStream(tempOriginalPath, FileMode.Create, FileAccess.Write))
                {
                    await videoStream.CopyToAsync(fileStream);
                }
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458

                // 5. Cifrar video usando VideoEncryptionService
                var nombreArchivoCifrado = $"{Guid.NewGuid()}.encrypted";
                var rutaAlmacenamiento = Path.Combine(_videosPath, nombreArchivoCifrado);

                var encryptionResult = await _videoEncryptionService.EncryptVideoAsync(
                    tempOriginalPath,
                    rutaAlmacenamiento,
                    userKeys.ClaveHMAC,
                    serverPublicKey
                );

                // 6. Eliminar archivo temporal
                if (File.Exists(tempOriginalPath))
                {
                    File.Delete(tempOriginalPath);
                }

                // 7. Crear registro de video
                var video = new Video
                {
                    IdAdministrador = request.IdAdministrador,
                    TituloVideo = request.NombreArchivo,
                    Descripcion = request.Descripcion,
                    NombreArchivoOriginal = request.NombreArchivo,
                    NombreArchivoCifrado = nombreArchivoCifrado,
                    TamañoArchivo = encryptionResult.EncryptedSizeBytes,
                    RutaAlmacenamiento = rutaAlmacenamiento,
                    EstadoProcesamiento = "Disponible",
                    FechaSubida = DateTime.UtcNow
                };

                _context.Videos.Add(video);
                await _context.SaveChangesAsync();

                // 8. Crear registro de datos criptográficos
                var cryptoData = new CryptoData
                {
                    IdVideo = video.IdVideo,
                    KEKCifrada = encryptionResult.EncryptedKek,
                    AlgoritmoKEK = "RSA-OAEP",
                    Nonce = encryptionResult.Nonce,
                    AuthTag = encryptionResult.AuthTag,
                    HashSHA256Original = encryptionResult.OriginalHash,
                    HMACDelVideo = encryptionResult.HmacOfEncryptedVideo,
                    FechaGeneracionClaves = DateTime.UtcNow,
                    VersionAlgoritmo = "1.0"
                };

                _context.DatosCriptograficosVideos.Add(cryptoData);
                await _context.SaveChangesAsync();

                var response = new VideoResponse
                {
                    IdVideo = video.IdVideo,
                    TituloVideo = video.TituloVideo,
                    Descripcion = video.Descripcion,
                    NombreArchivoOriginal = video.NombreArchivoOriginal,
                    TamañoArchivo = video.TamañoArchivo,
                    EstadoProcesamiento = video.EstadoProcesamiento,
                    FechaSubida = video.FechaSubida,
                    IdAdministrador = request.IdAdministrador,
                    NombreAdministrador = admin.NombreUsuario,
                    Message = "Video subido y cifrado exitosamente"
                };

                return ApiResponse<VideoResponse>.SuccessResponse(response, "Video subido exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<VideoResponse>.ErrorResponse($"Error al subir video: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VideoListResponse>>> GetAllVideosAsync()
        {
            try
            {
                var videos = await _context.Videos
                    .Include(v => v.Administrador)
                    .Where(v => v.EstadoProcesamiento == "Disponible")
                    .OrderByDescending(v => v.FechaSubida)
                    .ToListAsync();

                var videoList = videos.Select(v => new VideoListResponse
                {
                    IdVideo = v.IdVideo,
                    TituloVideo = v.TituloVideo,
                    Descripcion = v.Descripcion,
                    TamañoArchivo = v.TamañoArchivo,
                    Duracion = v.Duracion,
                    FormatoVideo = v.FormatoVideo,
                    EstadoProcesamiento = v.EstadoProcesamiento,
                    FechaSubida = v.FechaSubida,
                    NombreAdministrador = v.Administrador.NombreUsuario
                }).ToList();

                return ApiResponse<List<VideoListResponse>>.SuccessResponse(videoList);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VideoListResponse>>.ErrorResponse($"Error al obtener videos: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VideoListResponse>>> GetVideosByAdminAsync(int adminId)
        {
            try
            {
                var videos = await _context.Videos
                    .Include(v => v.Administrador)
                    .Where(v => v.IdAdministrador == adminId && v.EstadoProcesamiento != "Eliminado")
                    .OrderByDescending(v => v.FechaSubida)
                    .ToListAsync();

                var videoList = videos.Select(v => new VideoListResponse
                {
                    IdVideo = v.IdVideo,
                    TituloVideo = v.TituloVideo,
                    Descripcion = v.Descripcion,
                    TamañoArchivo = v.TamañoArchivo,
                    Duracion = v.Duracion,
                    FormatoVideo = v.FormatoVideo,
                    EstadoProcesamiento = v.EstadoProcesamiento,
                    FechaSubida = v.FechaSubida,
                    NombreAdministrador = v.Administrador.NombreUsuario
                }).ToList();

                return ApiResponse<List<VideoListResponse>>.SuccessResponse(videoList);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VideoListResponse>>.ErrorResponse($"Error al obtener videos: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VideoResponse>> GetVideoByIdAsync(int videoId)
        {
            try
            {
                var video = await _context.Videos
                    .Include(v => v.Administrador)
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("Video no encontrado");
                }

                var response = new VideoResponse
                {
                    IdVideo = video.IdVideo,
                    TituloVideo = video.TituloVideo,
                    Descripcion = video.Descripcion,
                    NombreArchivoOriginal = video.NombreArchivoOriginal,
                    TamañoArchivo = video.TamañoArchivo,
                    Duracion = video.Duracion,
                    FormatoVideo = video.FormatoVideo,
                    EstadoProcesamiento = video.EstadoProcesamiento,
                    FechaSubida = video.FechaSubida,
                    IdAdministrador = video.IdAdministrador,
                    NombreAdministrador = video.Administrador.NombreUsuario
                };

                return ApiResponse<VideoResponse>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<VideoResponse>.ErrorResponse($"Error al obtener video: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteVideoAsync(int videoId, int adminId)
        {
            try
            {
                var video = await _context.Videos.FindAsync(videoId);

                if (video == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Video no encontrado");
                }

                // Verificar que el administrador es el dueño del video
                if (video.IdAdministrador != adminId)
                {
                    return ApiResponse<bool>.ErrorResponse("Solo el administrador dueño puede eliminar el video");
                }

                // Soft delete
                video.EstadoProcesamiento = "Eliminado";
                video.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Opcional: eliminar archivo físico
                // if (File.Exists(video.RutaAlmacenamiento))
                // {
                //     File.Delete(video.RutaAlmacenamiento);
                // }

                return ApiResponse<bool>.SuccessResponse(true, "Video eliminado exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error al eliminar video: {ex.Message}");
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Obtener o crear la clave pública del servidor (persistente)
        /// Esta clave se usa para cifrar las KEKs de los videos
        /// </summary>
        private async Task<string> GetOrCreateServerPublicKeyAsync()
        {
            // Si la clave pública existe, leerla
            if (File.Exists(_serverPublicKeyPath))
            {
                return await File.ReadAllTextAsync(_serverPublicKeyPath);
            }

            // Si no existe, generar el par de claves
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);
            
            // Guardar clave pública
            await File.WriteAllTextAsync(_serverPublicKeyPath, publicKey);
            
            // Guardar clave privada
            var privateKeyPath = Path.Combine(_keysPath, "server_private_key.pem");
            await File.WriteAllTextAsync(privateKeyPath, privateKey);

            return publicKey;
=======
        public async Task<ApiResponse<VideoIntegrityResponse>> VerifyVideoIntegrityAsync(int videoId, int adminId)
        {
            try
            {
                var video = await _context.Videos.FindAsync(videoId);
                if (video == null)
                {
                    return ApiResponse<VideoIntegrityResponse>.ErrorResponse("Video no encontrado");
                }

                // Verificar que el administrador es el dueño del video
                if (video.IdAdministrador != adminId)
                {
                    return ApiResponse<VideoIntegrityResponse>.ErrorResponse("Solo el administrador dueño puede verificar la integridad");
                }

                // Obtener datos criptográficos
                var cryptoData = await _context.DatosCriptograficosVideos
                    .FirstOrDefaultAsync(c => c.IdVideo == videoId);

                if (cryptoData == null)
                {
                    return ApiResponse<VideoIntegrityResponse>.ErrorResponse("No se encontraron datos criptográficos para este video");
                }

                // Obtener clave HMAC del administrador
                var userKeys = await _context.ClavesUsuarios
                    .FirstOrDefaultAsync(k => k.IdUsuario == adminId);

                if (userKeys?.ClaveHMAC == null)
                {
                    return ApiResponse<VideoIntegrityResponse>.ErrorResponse("No se encontró la clave HMAC del administrador");
                }

                // Verificar integridad del video
                var isValid = _videoEncryptionService.VerifyVideoIntegrity(
                    video.RutaAlmacenamiento,
                    cryptoData.HMACDelVideo,
                    userKeys.ClaveHMAC
                );

                var response = new VideoIntegrityResponse
                {
                    IdVideo = videoId,
                    TituloVideo = video.TituloVideo,
                    IsValid = isValid,
                    HashSHA256Original = Convert.ToBase64String(cryptoData.HashSHA256Original),
                    FechaVerificacion = DateTime.UtcNow,
                    Message = isValid 
                        ? "La integridad del video es válida" 
                        : "ALERTA: La integridad del video ha sido comprometida"
                };

                return ApiResponse<VideoIntegrityResponse>.SuccessResponse(
                    response, 
                    isValid ? "Verificación exitosa" : "Verificación fallida"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<VideoIntegrityResponse>.ErrorResponse($"Error al verificar integridad: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VideoResponse>> UpdateVideoMetadataAsync(int videoId, UpdateVideoMetadataRequest request, int adminId)
        {
            try
            {
                var video = await _context.Videos
                    .Include(v => v.Administrador)
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("Video no encontrado");
                }

                // Verificar que el administrador es el dueño del video
                if (video.IdAdministrador != adminId)
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("Solo el administrador dueño puede actualizar el video");
                }

                // Actualizar metadata
                if (!string.IsNullOrWhiteSpace(request.TituloVideo))
                {
                    video.TituloVideo = request.TituloVideo;
                }

                if (!string.IsNullOrWhiteSpace(request.Descripcion))
                {
                    video.Descripcion = request.Descripcion;
                }

                video.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = new VideoResponse
                {
                    IdVideo = video.IdVideo,
                    TituloVideo = video.TituloVideo,
                    Descripcion = video.Descripcion,
                    NombreArchivoOriginal = video.NombreArchivoOriginal,
                    TamañoArchivo = video.TamañoArchivo,
                    EstadoProcesamiento = video.EstadoProcesamiento,
                    FechaSubida = video.FechaSubida,
                    IdAdministrador = video.IdAdministrador,
                    NombreAdministrador = video.Administrador.NombreUsuario,
                    Message = "Metadata actualizada exitosamente"
                };

                return ApiResponse<VideoResponse>.SuccessResponse(response, "Metadata actualizada exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<VideoResponse>.ErrorResponse($"Error al actualizar metadata: {ex.Message}");
            }
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
        }
    }
}
