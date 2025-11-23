using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Exceptions;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PermissionResponse>> RequestAccessAsync(int videoId, int userId, string justificacion)
        {
            // Validar que el video existe
            var video = await _context.Videos
                .FirstOrDefaultAsync(v => v.IdVideo == videoId && v.EstadoProcesamiento != "Eliminado")
                ?? throw new VideoNotFoundException($"Video con ID {videoId} no encontrado");

            // Validar que el usuario existe
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == userId)
                ?? throw new UserNotFoundException($"Usuario con ID {userId} no encontrado");

            // Verificar que el usuario no sea el dueño del video
            if (video.IdAdministrador == userId)
            {
                throw new InvalidOperationException("No puedes solicitar acceso a tu propio video");
            }

            // Verificar si ya existe una solicitud pendiente o aprobada
            var existingPermission = await _context.Permisos
                .Where(p => p.IdVideo == videoId && p.IdUsuario == userId)
                .OrderByDescending(p => p.FechaOtorgamiento)
                .FirstOrDefaultAsync();

            if (existingPermission != null)
            {
                if (existingPermission.TipoPermiso == "Pendiente")
                {
                    throw new InvalidOperationException("Ya tienes una solicitud pendiente para este video");
                }

                if (existingPermission.TipoPermiso == "Aprobado" && 
                    (!existingPermission.FechaExpiracion.HasValue || existingPermission.FechaExpiracion > DateTime.UtcNow))
                {
                    throw new InvalidOperationException("Ya tienes acceso aprobado a este video");
                }
            }

            // Crear nueva solicitud
            var permiso = new Permission
            {
                IdVideo = videoId,
                IdUsuario = userId,
                TipoPermiso = "Pendiente",
                FechaOtorgamiento = DateTime.UtcNow,
                NumeroAccesos = 0
            };

            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return ApiResponse<PermissionResponse>.SuccessResponse(
                MapToResponse(permiso, video, usuario), 
                "Solicitud de acceso creada exitosamente");
        }

        public async Task<ApiResponse<bool>> HasAccessAsync(int videoId, int userId)
        {
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => 
                    p.IdVideo == videoId && 
                    p.IdUsuario == userId && 
                    p.TipoPermiso == "Aprobado" &&
                    !p.FechaRevocacion.HasValue);

            if (permiso == null) 
                return ApiResponse<bool>.SuccessResponse(false, "No tiene permiso para este video");

            // Verificar expiración
            if (permiso.FechaExpiracion.HasValue && permiso.FechaExpiracion < DateTime.UtcNow)
            {
                return ApiResponse<bool>.SuccessResponse(false, "El permiso ha expirado");
            }

            // Verificar límite de accesos si está configurado
            if (permiso.MaxAccesos.HasValue && permiso.NumeroAccesos >= permiso.MaxAccesos)
            {
                return ApiResponse<bool>.SuccessResponse(false, "Límite de accesos alcanzado");
            }

            return ApiResponse<bool>.SuccessResponse(true, "Acceso autorizado");
        }

        public async Task<ApiResponse<PermissionResponse>> GetPermissionAsync(int videoId, int userId)
        {
            var permiso = await _context.Permisos
                .Include(p => p.Video)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => 
                    p.IdVideo == videoId && 
                    p.IdUsuario == userId &&
                    p.TipoPermiso == "Aprobado");

            if (permiso == null) 
                return ApiResponse<PermissionResponse>.ErrorResponse("Permiso no encontrado");

            return ApiResponse<PermissionResponse>.SuccessResponse(
                MapToResponse(permiso, permiso.Video, permiso.Usuario));
        }

        public async Task<ApiResponse<PermissionResponse>> ApproveAccessAsync(
            int permisoId, 
            int adminId, 
            int? maxAccesos = null, 
            DateTime? fechaExpiracion = null)
        {
            var permiso = await _context.Permisos
                .Include(p => p.Video)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.IdPermiso == permisoId)
                ?? throw new KeyNotFoundException($"Permiso con ID {permisoId} no encontrado");

            // Verificar que el admin sea el dueño del video
            if (permiso.Video.IdAdministrador != adminId)
            {
                throw new VideoNotOwnedException("Solo el dueño del video puede aprobar accesos");
            }

            // Verificar estado pendiente
            if (permiso.TipoPermiso != "Pendiente")
            {
                throw new InvalidOperationException("Solo se pueden aprobar permisos en estado Pendiente");
            }

            // Aprobar permiso
            permiso.TipoPermiso = "Aprobado";
            permiso.MaxAccesos = maxAccesos;
            permiso.FechaExpiracion = fechaExpiracion;

            await _context.SaveChangesAsync();

            return ApiResponse<PermissionResponse>.SuccessResponse(
                MapToResponse(permiso, permiso.Video, permiso.Usuario),
                "Acceso aprobado exitosamente");
        }

        public async Task<ApiResponse<bool>> RevokeAccessAsync(int permisoId, int adminId)
        {
            var permiso = await _context.Permisos
                .Include(p => p.Video)
                .FirstOrDefaultAsync(p => p.IdPermiso == permisoId)
                ?? throw new KeyNotFoundException($"Permiso con ID {permisoId} no encontrado");

            // Verificar que el admin sea el dueño del video
            if (permiso.Video.IdAdministrador != adminId)
            {
                throw new VideoNotOwnedException("Solo el dueño del video puede revocar accesos");
            }

            permiso.TipoPermiso = "Revocado";
            permiso.FechaRevocacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Acceso revocado exitosamente");
        }

        public async Task<ApiResponse<List<PermissionResponse>>> GetVideoPermissionsAsync(int videoId, int adminId)
        {
            // Verificar que el admin sea el dueño del video
            var video = await _context.Videos
                .FirstOrDefaultAsync(v => v.IdVideo == videoId && v.IdAdministrador == adminId)
                ?? throw new VideoNotOwnedException("Solo el dueño del video puede ver sus permisos");

            var permisos = await _context.Permisos
                .Include(p => p.Video)
                .Include(p => p.Usuario)
                .Where(p => p.IdVideo == videoId)
                .OrderByDescending(p => p.FechaOtorgamiento)
                .ToListAsync();

            var responses = permisos.Select(p => MapToResponse(p, p.Video, p.Usuario)).ToList();
            return ApiResponse<List<PermissionResponse>>.SuccessResponse(responses);
        }

        public async Task<ApiResponse<List<PermissionResponse>>> GetMyPermissionsAsync(int userId)
        {
            var permisos = await _context.Permisos
                .Include(p => p.Video)
                .Include(p => p.Usuario)
                .Where(p => p.IdUsuario == userId)
                .OrderByDescending(p => p.FechaOtorgamiento)
                .ToListAsync();

            var responses = permisos.Select(p => MapToResponse(p, p.Video, p.Usuario)).ToList();
            return ApiResponse<List<PermissionResponse>>.SuccessResponse(responses);
        }

        public async Task RegisterAccessAsync(int videoId, int userId, bool exitoso, string? mensajeError = null)
        {
            var registroAcceso = new AccessLog
            {
                IdVideo = videoId,
                IdUsuario = userId,
                FechaHoraAcceso = DateTime.UtcNow,
                TipoAcceso = "SolicitudClave",
                Exitoso = exitoso,
                MensajeError = mensajeError
            };

            _context.RegistroAccesos.Add(registroAcceso);
            await _context.SaveChangesAsync();
        }

        public async Task IncrementAccessCountAsync(int permisoId)
        {
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.IdPermiso == permisoId)
                ?? throw new KeyNotFoundException($"Permiso con ID {permisoId} no encontrado");

            permiso.NumeroAccesos++;
            await _context.SaveChangesAsync();
        }

        private static PermissionResponse MapToResponse(Permission permiso, Video video, User usuario)
        {
            var estaExpirado = permiso.FechaExpiracion.HasValue && permiso.FechaExpiracion < DateTime.UtcNow;
            var accesosAgotados = permiso.MaxAccesos.HasValue && permiso.NumeroAccesos >= permiso.MaxAccesos;
            var estaActivo = permiso.TipoPermiso == "Aprobado" && 
                            !permiso.FechaRevocacion.HasValue && 
                            !estaExpirado && 
                            !accesosAgotados;

            string? mensajeEstado = null;
            if (permiso.TipoPermiso == "Pendiente")
                mensajeEstado = "Solicitud pendiente de aprobación";
            else if (permiso.TipoPermiso == "Revocado")
                mensajeEstado = "Acceso revocado";
            else if (estaExpirado)
                mensajeEstado = "Acceso expirado";
            else if (accesosAgotados)
                mensajeEstado = "Límite de accesos alcanzado";
            else if (estaActivo)
                mensajeEstado = "Acceso activo";

            return new PermissionResponse
            {
                IdPermiso = permiso.IdPermiso,
                IdVideo = permiso.IdVideo,
                TituloVideo = video.TituloVideo,
                IdUsuario = permiso.IdUsuario,
                NombreUsuario = usuario.NombreUsuario,
                TipoPermiso = permiso.TipoPermiso,
                FechaOtorgamiento = permiso.FechaOtorgamiento,
                FechaExpiracion = permiso.FechaExpiracion,
                FechaRevocacion = permiso.FechaRevocacion,
                NumeroAccesos = permiso.NumeroAccesos,
                AccesosRestantes = permiso.MaxAccesos.HasValue 
                    ? Math.Max(0, permiso.MaxAccesos.Value - permiso.NumeroAccesos)
                    : null,
                EstaActivo = estaActivo,
                EstaExpirado = estaExpirado,
                MensajeEstado = mensajeEstado
            };
        }
    }
}
