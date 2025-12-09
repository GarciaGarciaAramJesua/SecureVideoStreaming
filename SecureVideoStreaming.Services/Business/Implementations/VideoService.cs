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
        private readonly IChaCha20Poly1305Service _chaCha20Service;
        private readonly IRsaService _rsaService;
        private readonly IConfiguration _configuration;
        private readonly string _videosPath;
        private readonly string _keysPath;
        private readonly string _serverPublicKeyPath;

        public VideoService(
            ApplicationDbContext context,
            IVideoEncryptionService videoEncryptionService,
            IKeyManagementService keyManagementService,
            IChaCha20Poly1305Service chaCha20Service,
            IRsaService rsaService,
            IConfiguration configuration)
        {
            _context = context;
            _videoEncryptionService = videoEncryptionService;
            _keyManagementService = keyManagementService;
            _chaCha20Service = chaCha20Service;
            _rsaService = rsaService;
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
                // 4. Guardar video original temporalmente
                var tempOriginalPath = Path.Combine(_videosPath, $"temp_{Guid.NewGuid()}.tmp");
                await using (var fileStream = new FileStream(tempOriginalPath, FileMode.Create, FileAccess.Write))
                {
                    await videoStream.CopyToAsync(fileStream);
                }

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

        /// <summary>
        /// Obtener o crear la clave pública del servidor (persistente)
        /// Esta clave se usa para cifrar las KEKs de los videos
        /// </summary>
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

                // Determinar la ruta completa del archivo cifrado
                string encryptedFilePath;
                if (Path.IsPathRooted(video.RutaAlmacenamiento) || 
                    video.RutaAlmacenamiento.StartsWith("./") || 
                    video.RutaAlmacenamiento.StartsWith(".\\"))
                {
                    encryptedFilePath = video.RutaAlmacenamiento;
                }
                else
                {
                    encryptedFilePath = Path.Combine(_videosPath, video.RutaAlmacenamiento);
                }

                Console.WriteLine($"[VerifyVideoIntegrityAsync] Video ID: {videoId}");
                Console.WriteLine($"[VerifyVideoIntegrityAsync] RutaAlmacenamiento de BD: {video.RutaAlmacenamiento}");
                Console.WriteLine($"[VerifyVideoIntegrityAsync] _videosPath: {_videosPath}");
                Console.WriteLine($"[VerifyVideoIntegrityAsync] Ruta completa computada: {encryptedFilePath}");
                Console.WriteLine($"[VerifyVideoIntegrityAsync] Archivo existe: {File.Exists(encryptedFilePath)}");

                // Verificar que el archivo existe antes de verificar integridad
                if (!File.Exists(encryptedFilePath))
                {
                    return ApiResponse<VideoIntegrityResponse>.ErrorResponse($"Archivo cifrado no encontrado en: {encryptedFilePath}");
                }

                // Verificar integridad del video
                var isValid = _videoEncryptionService.VerifyVideoIntegrity(
                    encryptedFilePath,
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
        }

        public async Task<ApiResponse<EncryptedVideoDataResponse>> GetEncryptedVideoDataAsync(int videoId)
        {
            try
            {
                Console.WriteLine($"[VideoService] Obteniendo video cifrado - VideoId: {videoId}");
                
                // 1. Obtener video de la base de datos
                var video = await _context.Videos
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    Console.WriteLine($"[VideoService] Video {videoId} no encontrado en la base de datos");
                    return ApiResponse<EncryptedVideoDataResponse>.ErrorResponse("Video no encontrado");
                }

                Console.WriteLine($"[VideoService] Video encontrado: {video.TituloVideo}");
                Console.WriteLine($"[VideoService] RutaAlmacenamiento: {video.RutaAlmacenamiento}");
                Console.WriteLine($"[VideoService] _videosPath: {_videosPath}");

                // 2. Verificar que el archivo cifrado existe
                // IMPORTANTE: RutaAlmacenamiento puede ser ruta completa o solo nombre de archivo
                string encryptedFilePath;
                if (Path.IsPathRooted(video.RutaAlmacenamiento) || video.RutaAlmacenamiento.StartsWith("./") || video.RutaAlmacenamiento.StartsWith(".\\"))
                {
                    // RutaAlmacenamiento ya es una ruta completa o relativa
                    encryptedFilePath = video.RutaAlmacenamiento;
                    Console.WriteLine($"[VideoService] RutaAlmacenamiento es ruta completa/relativa");
                }
                else
                {
                    // RutaAlmacenamiento es solo el nombre del archivo
                    encryptedFilePath = Path.Combine(_videosPath, video.RutaAlmacenamiento);
                    Console.WriteLine($"[VideoService] RutaAlmacenamiento es nombre de archivo, combinando con _videosPath");
                }
                
                Console.WriteLine($"[VideoService] Ruta completa del archivo: {encryptedFilePath}");
                Console.WriteLine($"[VideoService] Archivo existe: {File.Exists(encryptedFilePath)}");

                if (!File.Exists(encryptedFilePath))
                {
                    // Listar archivos en el directorio para debugging
                    if (Directory.Exists(_videosPath))
                    {
                        var files = Directory.GetFiles(_videosPath);
                        Console.WriteLine($"[VideoService] Archivos en {_videosPath}:");
                        foreach (var file in files)
                        {
                            Console.WriteLine($"  - {Path.GetFileName(file)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[VideoService] El directorio {_videosPath} no existe");
                    }

                    return ApiResponse<EncryptedVideoDataResponse>.ErrorResponse("Archivo de video no encontrado");
                }

                // 3. Leer datos del archivo cifrado
                var encryptedBytes = await File.ReadAllBytesAsync(encryptedFilePath);

                // 4. Leer nonce y auth tag del archivo metadata
                var metadataPath = encryptedFilePath + ".metadata";
                if (!File.Exists(metadataPath))
                {
                    return ApiResponse<EncryptedVideoDataResponse>.ErrorResponse("Metadata del video no encontrada");
                }

                var metadata = await File.ReadAllTextAsync(metadataPath);
                var metadataLines = metadata.Split('\n');
                
                string nonceBase64 = string.Empty;
                string authTagBase64 = string.Empty;

                foreach (var line in metadataLines)
                {
                    if (line.StartsWith("Nonce:"))
                    {
                        nonceBase64 = line.Replace("Nonce:", "").Trim();
                    }
                    else if (line.StartsWith("AuthTag:"))
                    {
                        authTagBase64 = line.Replace("AuthTag:", "").Trim();
                    }
                }

                if (string.IsNullOrEmpty(nonceBase64) || string.IsNullOrEmpty(authTagBase64))
                {
                    return ApiResponse<EncryptedVideoDataResponse>.ErrorResponse("Metadata incompleta");
                }

                // 5. Crear respuesta
                var response = new EncryptedVideoDataResponse
                {
                    Ciphertext = Convert.ToBase64String(encryptedBytes),
                    Nonce = nonceBase64,
                    AuthTag = authTagBase64,
                    OriginalSize = video.TamañoArchivo,
                    Format = video.FormatoVideo ?? "mp4"
                };

                return ApiResponse<EncryptedVideoDataResponse>.SuccessResponse(response, "Video cifrado obtenido correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<EncryptedVideoDataResponse>.ErrorResponse($"Error al obtener video cifrado: {ex.Message}");
            }
        }

        public async Task<DecryptedVideoStreamResponse> GetDecryptedVideoStreamAsync(int videoId, int userId)
        {
            try
            {
                // 1. Verificar que el usuario existe
                var user = await _context.Usuarios.FindAsync(userId);
                if (user == null)
                {
                    throw new UnauthorizedAccessException("Usuario no encontrado");
                }

                // 2. Obtener el video de la base de datos
                var video = await _context.Videos
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    throw new FileNotFoundException("Video no encontrado");
                }

                if (video.EstadoProcesamiento != "Disponible")
                {
                    throw new InvalidOperationException("El video no está disponible");
                }

                // 3. Verificar permisos del usuario para ver el video
                Console.WriteLine($"[VideoService] Verificando permisos - UserId: {userId}, VideoId: {videoId}");
                Console.WriteLine($"[VideoService] TipoUsuario: {user.TipoUsuario}, IdAdministrador del video: {video.IdAdministrador}");
                
                // Si el usuario es administrador y es dueño del video, tiene acceso
                if (user.TipoUsuario == "Administrador" && video.IdAdministrador == userId)
                {
                    Console.WriteLine($"[VideoService] ✓ Acceso concedido: Administrador dueño del video");
                }
                // Si NO es administrador dueño, verificar permisos explícitos (aplica para consumidores y usuarios normales)
                else
                {
                    var permission = await _context.Permisos
                        .FirstOrDefaultAsync(p => p.IdUsuario == userId && p.IdVideo == videoId);
                    
                    if (permission == null)
                    {
                        Console.WriteLine($"[VideoService] ✗ No existe permiso para este usuario y video");
                        throw new UnauthorizedAccessException("No tienes permiso para ver este video");
                    }
                    
                    Console.WriteLine($"[VideoService] Permiso encontrado - TipoPermiso: {permission.TipoPermiso}");
                    
                    // Verificar que el permiso no esté revocado
                    if (permission.TipoPermiso == "Revocado")
                    {
                        Console.WriteLine($"[VideoService] ✗ Permiso revocado");
                        throw new UnauthorizedAccessException("Tu permiso para ver este video ha sido revocado");
                    }
                    
                    // Verificar que el permiso esté aprobado (si aplica el sistema de aprobación)
                    if (permission.TipoPermiso == "Pendiente")
                    {
                        Console.WriteLine($"[VideoService] ✗ Permiso pendiente de aprobación");
                        throw new UnauthorizedAccessException("Tu permiso está pendiente de aprobación");
                    }
                    
                    // Verificar si ha expirado
                    if (permission.FechaExpiracion.HasValue && permission.FechaExpiracion.Value < DateTime.UtcNow)
                    {
                        Console.WriteLine($"[VideoService] ✗ Permiso expirado");
                        throw new UnauthorizedAccessException("Tu permiso ha expirado");
                    }
                    
                    // Verificar límite de accesos
                    if (permission.MaxAccesos.HasValue && permission.NumeroAccesos >= permission.MaxAccesos.Value)
                    {
                        Console.WriteLine($"[VideoService] ✗ Límite de accesos alcanzado");
                        throw new UnauthorizedAccessException("Has alcanzado el límite de accesos permitidos");
                    }
                    
                    Console.WriteLine($"[VideoService] ✓ Acceso concedido: Usuario con permiso válido ({user.TipoUsuario})");
                    
                    // Actualizar contador de accesos y última fecha de acceso
                    permission.NumeroAccesos++;
                    permission.UltimoAcceso = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // 4. Obtener los datos criptográficos del video
                var cryptoData = await _context.DatosCriptograficosVideos
                    .FirstOrDefaultAsync(c => c.IdVideo == videoId);

                if (cryptoData == null)
                {
                    throw new InvalidOperationException("Datos criptográficos no encontrados");
                }

                // 5. Determinar la ruta completa del archivo cifrado
                string encryptedFilePath;
                if (Path.IsPathRooted(video.RutaAlmacenamiento) || 
                    video.RutaAlmacenamiento.StartsWith("./") || 
                    video.RutaAlmacenamiento.StartsWith(".\\"))
                {
                    encryptedFilePath = video.RutaAlmacenamiento;
                }
                else
                {
                    encryptedFilePath = Path.Combine(_videosPath, video.RutaAlmacenamiento);
                }

                if (!File.Exists(encryptedFilePath))
                {
                    throw new FileNotFoundException($"Archivo cifrado no encontrado: {encryptedFilePath}");
                }

                // 6. Leer el video cifrado
                var encryptedBytes = await File.ReadAllBytesAsync(encryptedFilePath);

                // 7. Obtener la clave privada del servidor para descifrar la KEK
                var serverPrivateKey = _keyManagementService.GetServerPrivateKey();

                // 8. Descifrar la KEK (Key Encryption Key) usando RSA
                byte[] kek = _rsaService.Decrypt(cryptoData.KEKCifrada, serverPrivateKey);

                // 9. Descifrar el video usando ChaCha20-Poly1305 con la KEK
                byte[] decryptedBytes = _chaCha20Service.Decrypt(
                    encryptedBytes,
                    kek,
                    cryptoData.Nonce,
                    cryptoData.AuthTag,
                    cryptoData.AAD
                );

                // 10. Crear stream con los datos descifrados
                var memoryStream = new MemoryStream(decryptedBytes);

                // 11. Retornar la respuesta con el stream descifrado
                return new DecryptedVideoStreamResponse
                {
                    Stream = memoryStream,
                    ContentType = $"video/{video.FormatoVideo ?? "mp4"}",
                    Length = decryptedBytes.Length
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al desencriptar el video: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene video cifrado + KEK cifrada para descifrado E2E en cliente
        /// Compatible con @stablelib/chacha20poly1305
        /// </summary>
        public async Task<ApiResponse<E2EVideoDataResponse>> GetEncryptedVideoForClientAsync(
            int videoId, 
            int userId, 
            string clientPublicKeyBase64)
        {
            try
            {
                // 1. Verificar que el video existe
                var video = await _context.Videos
                    .Include(v => v.DatosCriptograficos)
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    return ApiResponse<E2EVideoDataResponse>.ErrorResponse("Video no encontrado");
                }

                // 2. Verificar que el usuario tiene acceso
                // Un permiso está activo si:
                // - TipoPermiso es "Lectura" (no está "Revocado")
                // - No ha expirado (FechaExpiracion es null o en el futuro)
                // - No ha alcanzado el máximo de accesos (MaxAccesos es null o NumeroAccesos < MaxAccesos)
                var now = DateTime.UtcNow;
                var hasAccess = await _context.Permisos
                    .AnyAsync(p => p.IdVideo == videoId 
                        && p.IdUsuario == userId 
                        && p.TipoPermiso == "Lectura"
                        && (p.FechaExpiracion == null || p.FechaExpiracion > now)
                        && (p.MaxAccesos == null || p.NumeroAccesos < p.MaxAccesos));

                if (!hasAccess)
                {
                    return ApiResponse<E2EVideoDataResponse>.ErrorResponse("No tienes permiso para acceder a este video");
                }

                // 3. Obtener datos criptográficos
                var cryptoData = video.DatosCriptograficos;
                if (cryptoData == null)
                {
                    return ApiResponse<E2EVideoDataResponse>.ErrorResponse("Datos criptográficos no encontrados");
                }

                // 4. Leer el video cifrado desde el disco
                var encryptedFilePath = Path.IsPathRooted(video.RutaAlmacenamiento)
                    ? video.RutaAlmacenamiento
                    : Path.Combine(_videosPath, video.RutaAlmacenamiento);

                if (!File.Exists(encryptedFilePath))
                {
                    return ApiResponse<E2EVideoDataResponse>.ErrorResponse("Archivo de video no encontrado");
                }

                var encryptedVideoBytes = await File.ReadAllBytesAsync(encryptedFilePath);

                // 5. Descifrar la KEK con la clave privada del servidor
                var serverPrivateKey = _keyManagementService.GetServerPrivateKey();
                byte[] kek = _rsaService.Decrypt(cryptoData.KEKCifrada, serverPrivateKey);

                // 6. CRÍTICO: Re-cifrar la KEK con la clave pública del cliente (SPKI Base64)
                byte[] clientPublicKeyBytes = Convert.FromBase64String(clientPublicKeyBase64);
                byte[] encryptedKEKForClient = _rsaService.EncryptWithPublicKeyBytes(kek, clientPublicKeyBytes);

                // 7. Construir respuesta E2E
                var response = new E2EVideoDataResponse
                {
                    EncryptedVideo = Convert.ToBase64String(encryptedVideoBytes),
                    EncryptedKEK = Convert.ToBase64String(encryptedKEKForClient),
                    Nonce = Convert.ToBase64String(cryptoData.Nonce),
                    AuthTag = Convert.ToBase64String(cryptoData.AuthTag),
                    OriginalSize = video.TamañoArchivo,
                    Format = video.FormatoVideo ?? "mp4"
                };

                // 8. Limpiar KEK de memoria
                Array.Clear(kek, 0, kek.Length);

                return ApiResponse<E2EVideoDataResponse>.SuccessResponse(response, "Video cifrado obtenido para E2E");
            }
            catch (Exception ex)
            {
                return ApiResponse<E2EVideoDataResponse>.ErrorResponse($"Error al obtener video E2E: {ex.Message}");
            }
        }
    }
}
