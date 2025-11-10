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
        private readonly IChaCha20Poly1305Service _chaCha20Service;
        private readonly IRsaService _rsaService;
        private readonly IHashService _hashService;
        private readonly IHmacService _hmacService;
        private readonly IConfiguration _configuration;
        private readonly string _videosPath;

        public VideoService(
            ApplicationDbContext context,
            IChaCha20Poly1305Service chaCha20Service,
            IRsaService rsaService,
            IHashService hashService,
            IHmacService hmacService,
            IConfiguration configuration)
        {
            _context = context;
            _chaCha20Service = chaCha20Service;
            _rsaService = rsaService;
            _hashService = hashService;
            _hmacService = hmacService;
            _configuration = configuration;
            _videosPath = configuration["Storage:VideosPath"] ?? "./Storage/Videos";

            // Crear directorio si no existe
            if (!Directory.Exists(_videosPath))
            {
                Directory.CreateDirectory(_videosPath);
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

                // 2. Leer video en memoria
                using var memoryStream = new MemoryStream();
                await videoStream.CopyToAsync(memoryStream);
                var videoBytes = memoryStream.ToArray();

                // 3. Calcular hash del video original
                var hashOriginal = _hashService.ComputeSha256(videoBytes);

                // 4. Generar KEK (Key Encryption Key)
                var kek = _chaCha20Service.GenerateKey();

                // 5. Cifrar video con ChaCha20-Poly1305
                var (ciphertext, nonce, authTag) = _chaCha20Service.Encrypt(videoBytes, kek);

                // 6. Obtener clave HMAC del administrador
                var userKeys = await _context.ClavesUsuarios
                    .FirstOrDefaultAsync(k => k.IdUsuario == request.IdAdministrador);

                if (userKeys?.ClaveHMAC == null)
                {
                    return ApiResponse<VideoResponse>.ErrorResponse("El administrador no tiene clave HMAC configurada");
                }

                // 7. Calcular HMAC del video cifrado
                var hmac = _hmacService.ComputeHmac(ciphertext, userKeys.ClaveHMAC);

                // 8. Cifrar KEK con clave pública del servidor (simulado - en producción usar clave real)
                var (serverPublicKey, _) = _rsaService.GenerateKeyPair(2048);
                var encryptedKek = _rsaService.Encrypt(kek, serverPublicKey);

                // 9. Guardar video cifrado en disco
                var nombreArchivoCifrado = $"{Guid.NewGuid()}.encrypted";
                var rutaAlmacenamiento = Path.Combine(_videosPath, nombreArchivoCifrado);
                await File.WriteAllBytesAsync(rutaAlmacenamiento, ciphertext);

                // 10. Crear registro de video
                var video = new Video
                {
                    IdAdministrador = request.IdAdministrador,
                    TituloVideo = request.NombreArchivo,
                    Descripcion = request.Descripcion,
                    NombreArchivoOriginal = request.NombreArchivo,
                    NombreArchivoCifrado = nombreArchivoCifrado,
                    TamañoArchivo = ciphertext.Length,
                    RutaAlmacenamiento = rutaAlmacenamiento,
                    EstadoProcesamiento = "Disponible",
                    FechaSubida = DateTime.UtcNow
                };

                _context.Videos.Add(video);
                await _context.SaveChangesAsync();

                // 11. Crear registro de datos criptográficos
                var cryptoData = new CryptoData
                {
                    IdVideo = video.IdVideo,
                    KEKCifrada = encryptedKek,
                    AlgoritmoKEK = "ChaCha20-Poly1305",
                    Nonce = nonce,
                    AuthTag = authTag,
                    HashSHA256Original = hashOriginal,
                    HMACDelVideo = hmac,
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
    }
}
