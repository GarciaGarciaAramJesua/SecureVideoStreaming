using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class VideoGridService : IVideoGridService
    {
        private readonly ApplicationDbContext _context;

        public VideoGridService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<VideoGridItemResponse>>> GetVideoGridForUserAsync(int userId)
        {
            try
            {
                // Obtener todos los videos disponibles
                var videos = await _context.Videos
                    .Include(v => v.Administrador)
                    .Where(v => v.EstadoProcesamiento == "Disponible")
                    .OrderByDescending(v => v.FechaSubida)
                    .ToListAsync();

                // Obtener permisos activos del usuario
                var permisos = await _context.Permisos
                    .Where(p => p.IdUsuario == userId 
                        && p.FechaRevocacion == null)
                    .ToListAsync();

                var validPermissionTypes = new[] { "Aprobado", "Lectura", "Temporal" };

                var gridItems = videos.Select(video =>
                {
                    var permiso = permisos.FirstOrDefault(p => p.IdVideo == video.IdVideo);
                    bool tienePermiso = permiso != null && validPermissionTypes.Contains(permiso.TipoPermiso);
                    bool estaExpirado = permiso?.FechaExpiracion.HasValue == true 
                        && permiso.FechaExpiracion.Value < DateTime.UtcNow;
                    bool permiteVisualizacion = tienePermiso && !estaExpirado;

                    string estadoPermiso = tienePermiso
                        ? (estaExpirado ? "Expirado" : "Activo")
                        : "Sin Permiso";

                    return new VideoGridItemResponse
                    {
                        IdVideo = video.IdVideo,
                        TituloVideo = video.TituloVideo,
                        Descripcion = video.Descripcion,
                        TamañoArchivo = video.TamañoArchivo,
                        TamañoArchivoFormateado = FormatFileSize(video.TamañoArchivo),
                        Duracion = video.Duracion,
                        DuracionFormateada = FormatDuration(video.Duracion),
                        FormatoVideo = video.FormatoVideo,
                        FechaSubida = video.FechaSubida,
                        NombreAdministrador = video.Administrador.NombreUsuario,
                        AlgoritmoCifrado = "ChaCha20-Poly1305",
                        TienePermiso = tienePermiso,
                        IdPermiso = permiso?.IdPermiso,
                        TipoPermiso = permiso?.TipoPermiso,
                        FechaOtorgamiento = permiso?.FechaOtorgamiento,
                        FechaExpiracion = permiso?.FechaExpiracion,
                        NumeroAccesos = permiso?.NumeroAccesos ?? 0,
                        UltimoAcceso = permiso?.UltimoAcceso,
                        PermiteVisualizacion = permiteVisualizacion,
                        EstadoPermiso = estadoPermiso
                    };
                }).ToList();

                return ApiResponse<List<VideoGridItemResponse>>.SuccessResponse(gridItems);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VideoGridItemResponse>>.ErrorResponse($"Error al obtener grid de videos: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VideoGridItemResponse>>> GetVideoGridWithFiltersAsync(
            int userId,
            string? searchTerm = null,
            string? administrador = null,
            bool? soloConPermiso = null)
        {
            try
            {
                // Obtener videos con filtros
                var query = _context.Videos
                    .Include(v => v.Administrador)
                    .Where(v => v.EstadoProcesamiento == "Disponible");

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(v =>
                        v.TituloVideo.Contains(searchTerm) ||
                        (v.Descripcion != null && v.Descripcion.Contains(searchTerm)));
                }

                // Filtro por administrador
                if (!string.IsNullOrWhiteSpace(administrador))
                {
                    query = query.Where(v => v.Administrador.NombreUsuario.Contains(administrador));
                }

                var videos = await query.OrderByDescending(v => v.FechaSubida).ToListAsync();

                // Obtener permisos activos del usuario
                var permisos = await _context.Permisos
                    .Where(p => p.IdUsuario == userId && p.FechaRevocacion == null)
                    .ToListAsync();

                var validPermissionTypes = new[] { "Aprobado", "Lectura", "Temporal" };

                var gridItems = videos.Select(video =>
                {
                    var permiso = permisos.FirstOrDefault(p => p.IdVideo == video.IdVideo);
                    bool tienePermiso = permiso != null && validPermissionTypes.Contains(permiso.TipoPermiso);
                    bool estaExpirado = permiso?.FechaExpiracion.HasValue == true
                        && permiso.FechaExpiracion.Value < DateTime.UtcNow;
                    bool permiteVisualizacion = tienePermiso && !estaExpirado;

                    string estadoPermiso = tienePermiso
                        ? (estaExpirado ? "Expirado" : "Activo")
                        : "Sin Permiso";

                    return new VideoGridItemResponse
                    {
                        IdVideo = video.IdVideo,
                        TituloVideo = video.TituloVideo,
                        Descripcion = video.Descripcion,
                        TamañoArchivo = video.TamañoArchivo,
                        TamañoArchivoFormateado = FormatFileSize(video.TamañoArchivo),
                        Duracion = video.Duracion,
                        DuracionFormateada = FormatDuration(video.Duracion),
                        FormatoVideo = video.FormatoVideo,
                        FechaSubida = video.FechaSubida,
                        NombreAdministrador = video.Administrador.NombreUsuario,
                        AlgoritmoCifrado = "ChaCha20-Poly1305",
                        TienePermiso = tienePermiso,
                        IdPermiso = permiso?.IdPermiso,
                        TipoPermiso = permiso?.TipoPermiso,
                        FechaOtorgamiento = permiso?.FechaOtorgamiento,
                        FechaExpiracion = permiso?.FechaExpiracion,
                        NumeroAccesos = permiso?.NumeroAccesos ?? 0,
                        UltimoAcceso = permiso?.UltimoAcceso,
                        PermiteVisualizacion = permiteVisualizacion,
                        EstadoPermiso = estadoPermiso
                    };
                }).ToList();

                // Filtro solo con permiso
                if (soloConPermiso == true)
                {
                    gridItems = gridItems.Where(g => g.PermiteVisualizacion).ToList();
                }

                return ApiResponse<List<VideoGridItemResponse>>.SuccessResponse(gridItems);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VideoGridItemResponse>>.ErrorResponse($"Error al obtener grid filtrado: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VideoGridItemResponse>> GetVideoGridItemAsync(int videoId, int userId)
        {
            try
            {
                var video = await _context.Videos
                    .Include(v => v.Administrador)
                    .FirstOrDefaultAsync(v => v.IdVideo == videoId);

                if (video == null)
                {
                    return ApiResponse<VideoGridItemResponse>.ErrorResponse("Video no encontrado");
                }

                var permiso = await _context.Permisos
                    .FirstOrDefaultAsync(p => p.IdVideo == videoId 
                        && p.IdUsuario == userId 
                        && p.FechaRevocacion == null);

                bool tienePermiso = permiso != null;
                bool estaExpirado = permiso?.FechaExpiracion.HasValue == true
                    && permiso.FechaExpiracion.Value < DateTime.UtcNow;
                bool permiteVisualizacion = tienePermiso && !estaExpirado;

                string estadoPermiso = tienePermiso
                    ? (estaExpirado ? "Expirado" : "Activo")
                    : "Sin Permiso";

                var gridItem = new VideoGridItemResponse
                {
                    IdVideo = video.IdVideo,
                    TituloVideo = video.TituloVideo,
                    Descripcion = video.Descripcion,
                    TamañoArchivo = video.TamañoArchivo,
                    TamañoArchivoFormateado = FormatFileSize(video.TamañoArchivo),
                    Duracion = video.Duracion,
                    DuracionFormateada = FormatDuration(video.Duracion),
                    FormatoVideo = video.FormatoVideo,
                    FechaSubida = video.FechaSubida,
                    NombreAdministrador = video.Administrador.NombreUsuario,
                    AlgoritmoCifrado = "ChaCha20-Poly1305",
                    TienePermiso = tienePermiso,
                    IdPermiso = permiso?.IdPermiso,
                    TipoPermiso = permiso?.TipoPermiso,
                    FechaOtorgamiento = permiso?.FechaOtorgamiento,
                    FechaExpiracion = permiso?.FechaExpiracion,
                    NumeroAccesos = permiso?.NumeroAccesos ?? 0,
                    UltimoAcceso = permiso?.UltimoAcceso,
                    PermiteVisualizacion = permiteVisualizacion,
                    EstadoPermiso = estadoPermiso
                };

                return ApiResponse<VideoGridItemResponse>.SuccessResponse(gridItem);
            }
            catch (Exception ex)
            {
                return ApiResponse<VideoGridItemResponse>.ErrorResponse($"Error al obtener video: {ex.Message}");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string? FormatDuration(int? seconds)
        {
            if (!seconds.HasValue || seconds.Value <= 0)
                return null;

            var timeSpan = TimeSpan.FromSeconds(seconds.Value);
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            else
                return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }
}
