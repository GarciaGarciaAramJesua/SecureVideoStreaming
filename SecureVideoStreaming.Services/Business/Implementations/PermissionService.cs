using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Request;
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

        public async Task<ApiResponse<PermissionResponse>> GrantPermissionAsync(GrantPermissionRequest request)
        {
            try
            {
                // 1. Verificar que el video existe
                var video = await _context.Videos
                    .Include(v => v.Administrador)
                    .FirstOrDefaultAsync(v => v.IdVideo == request.IdVideo);

                if (video == null)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Video no encontrado");
                }

                // 2. Verificar que el video está disponible
                if (video.EstadoProcesamiento != "Disponible")
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("El video no está disponible");
                }

                // 3. Verificar que el otorgante es el administrador dueño del video
                if (video.IdAdministrador != request.OtorgadoPor)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Solo el administrador dueño puede otorgar permisos");
                }

                // 4. Verificar que el usuario consumidor existe y no es administrador
                var usuario = await _context.Usuarios.FindAsync(request.IdUsuario);
                if (usuario == null)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Usuario no encontrado");
                }

                if (usuario.TipoUsuario == "Administrador")
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("No se pueden otorgar permisos a administradores");
                }

                if (!usuario.Activo)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("El usuario no está activo");
                }

                // 5. Verificar que no existe un permiso activo para este usuario y video
                var existingPermission = await _context.Permisos
                    .FirstOrDefaultAsync(p => p.IdVideo == request.IdVideo 
                        && p.IdUsuario == request.IdUsuario 
                        && p.FechaRevocacion == null);

                if (existingPermission != null)
                {
                    // Verificar si está expirado
                    bool isExpired = existingPermission.FechaExpiracion.HasValue 
                        && existingPermission.FechaExpiracion.Value < DateTime.UtcNow;

                    if (!isExpired)
                    {
                        return ApiResponse<PermissionResponse>.ErrorResponse("El usuario ya tiene un permiso activo para este video");
                    }
                    
                    // Si está expirado, lo revocamos y creamos uno nuevo
                    existingPermission.FechaRevocacion = DateTime.UtcNow;
                    existingPermission.RevocadoPor = request.OtorgadoPor;
                }

                // 6. Validar fecha de expiración si es permiso temporal
                if (request.TipoPermiso == "Temporal")
                {
                    if (!request.FechaExpiracion.HasValue)
                    {
                        return ApiResponse<PermissionResponse>.ErrorResponse("Los permisos temporales requieren una fecha de expiración");
                    }

                    if (request.FechaExpiracion.Value <= DateTime.UtcNow)
                    {
                        return ApiResponse<PermissionResponse>.ErrorResponse("La fecha de expiración debe ser futura");
                    }
                }

                // 7. Crear nuevo permiso
                var permission = new Permission
                {
                    IdVideo = request.IdVideo,
                    IdUsuario = request.IdUsuario,
                    TipoPermiso = request.TipoPermiso,
                    OtorgadoPor = request.OtorgadoPor,
                    FechaOtorgamiento = DateTime.UtcNow,
                    FechaExpiracion = request.TipoPermiso == "Temporal" ? request.FechaExpiracion : null,
                    NumeroAccesos = 0
                };

                _context.Permisos.Add(permission);
                await _context.SaveChangesAsync();

                // 8. Cargar relaciones para respuesta
                await _context.Entry(permission)
                    .Reference(p => p.Video)
                    .LoadAsync();
                await _context.Entry(permission)
                    .Reference(p => p.Usuario)
                    .LoadAsync();
                await _context.Entry(permission)
                    .Reference(p => p.UsuarioOtorgante)
                    .LoadAsync();

                var response = MapToResponse(permission);
                return ApiResponse<PermissionResponse>.SuccessResponse(response, "Permiso otorgado exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<PermissionResponse>.ErrorResponse($"Error al otorgar permiso: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> RevokePermissionAsync(int permissionId, int adminId)
        {
            try
            {
                var permission = await _context.Permisos
                    .Include(p => p.Video)
                    .FirstOrDefaultAsync(p => p.IdPermiso == permissionId);

                if (permission == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Permiso no encontrado");
                }

                // Verificar que el admin es el dueño del video
                if (permission.Video.IdAdministrador != adminId)
                {
                    return ApiResponse<bool>.ErrorResponse("Solo el administrador dueño puede revocar permisos");
                }

                // Verificar que no esté ya revocado
                if (permission.FechaRevocacion.HasValue)
                {
                    return ApiResponse<bool>.ErrorResponse("Este permiso ya ha sido revocado");
                }

                // Revocar
                permission.FechaRevocacion = DateTime.UtcNow;
                permission.RevocadoPor = adminId;
                permission.TipoPermiso = "Revocado";

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Permiso revocado exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error al revocar permiso: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CheckPermissionAsync(int videoId, int userId)
        {
            try
            {
                var permission = await _context.Permisos
                    .FirstOrDefaultAsync(p => p.IdVideo == videoId 
                        && p.IdUsuario == userId 
                        && p.FechaRevocacion == null);

                if (permission == null)
                {
                    return ApiResponse<bool>.SuccessResponse(false, "Usuario no tiene permiso");
                }

                // Verificar si está expirado
                if (permission.FechaExpiracion.HasValue && permission.FechaExpiracion.Value < DateTime.UtcNow)
                {
                    return ApiResponse<bool>.SuccessResponse(false, "Permiso expirado");
                }

                return ApiResponse<bool>.SuccessResponse(true, "Permiso activo");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error al verificar permiso: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<PermissionResponse>>> GetPermissionsByVideoAsync(int videoId, int adminId)
        {
            try
            {
                // Verificar que el video existe y pertenece al admin
                var video = await _context.Videos.FindAsync(videoId);
                if (video == null)
                {
                    return ApiResponse<List<PermissionResponse>>.ErrorResponse("Video no encontrado");
                }

                if (video.IdAdministrador != adminId)
                {
                    return ApiResponse<List<PermissionResponse>>.ErrorResponse("No tiene permisos para ver estos datos");
                }

                var permissions = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Include(p => p.UsuarioOtorgante)
                    .Include(p => p.UsuarioRevocador)
                    .Where(p => p.IdVideo == videoId)
                    .OrderByDescending(p => p.FechaOtorgamiento)
                    .ToListAsync();

                var response = permissions.Select(MapToResponse).ToList();
                return ApiResponse<List<PermissionResponse>>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PermissionResponse>>.ErrorResponse($"Error al obtener permisos: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<PermissionResponse>>> GetPermissionsByUserAsync(int userId)
        {
            try
            {
                var permissions = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Include(p => p.UsuarioOtorgante)
                    .Where(p => p.IdUsuario == userId 
                        && p.FechaRevocacion == null 
                        && (p.FechaExpiracion == null || p.FechaExpiracion > DateTime.UtcNow))
                    .OrderByDescending(p => p.FechaOtorgamiento)
                    .ToListAsync();

                var response = permissions.Select(MapToResponse).ToList();
                return ApiResponse<List<PermissionResponse>>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PermissionResponse>>.ErrorResponse($"Error al obtener permisos: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PermissionResponse>> ExtendPermissionAsync(int permissionId, DateTime newExpiration, int adminId)
        {
            try
            {
                var permission = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Include(p => p.UsuarioOtorgante)
                    .FirstOrDefaultAsync(p => p.IdPermiso == permissionId);

                if (permission == null)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Permiso no encontrado");
                }

                // Verificar que el admin es el dueño del video
                if (permission.Video.IdAdministrador != adminId)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Solo el administrador dueño puede extender permisos");
                }

                // Verificar que no esté revocado
                if (permission.FechaRevocacion.HasValue)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("No se puede extender un permiso revocado");
                }

                // Validar nueva fecha
                if (newExpiration <= DateTime.UtcNow)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("La nueva fecha de expiración debe ser futura");
                }

                permission.FechaExpiracion = newExpiration;
                permission.TipoPermiso = "Temporal";
                await _context.SaveChangesAsync();

                var response = MapToResponse(permission);
                return ApiResponse<PermissionResponse>.SuccessResponse(response, "Permiso extendido exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<PermissionResponse>.ErrorResponse($"Error al extender permiso: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PermissionResponse>> GetPermissionByIdAsync(int permissionId)
        {
            try
            {
                var permission = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Include(p => p.UsuarioOtorgante)
                    .Include(p => p.UsuarioRevocador)
                    .FirstOrDefaultAsync(p => p.IdPermiso == permissionId);

                if (permission == null)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Permiso no encontrado");
                }

                var response = MapToResponse(permission);
                return ApiResponse<PermissionResponse>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PermissionResponse>.ErrorResponse($"Error al obtener permiso: {ex.Message}");
            }
        }

        private PermissionResponse MapToResponse(Permission permission)
        {
            bool isExpired = permission.FechaExpiracion.HasValue 
                && permission.FechaExpiracion.Value < DateTime.UtcNow;
            bool isActive = !permission.FechaRevocacion.HasValue && !isExpired;

            return new PermissionResponse
            {
                IdPermiso = permission.IdPermiso,
                IdVideo = permission.IdVideo,
                TituloVideo = permission.Video?.TituloVideo ?? string.Empty,
                IdUsuario = permission.IdUsuario,
                NombreUsuario = permission.Usuario?.NombreUsuario ?? string.Empty,
                EmailUsuario = permission.Usuario?.Email ?? string.Empty,
                TipoPermiso = permission.TipoPermiso,
                FechaOtorgamiento = permission.FechaOtorgamiento,
                FechaExpiracion = permission.FechaExpiracion,
                FechaRevocacion = permission.FechaRevocacion,
                NumeroAccesos = permission.NumeroAccesos,
                UltimoAcceso = permission.UltimoAcceso,
                OtorgadoPor = permission.OtorgadoPor,
                NombreOtorgante = permission.UsuarioOtorgante?.NombreUsuario ?? string.Empty,
                RevocadoPor = permission.RevocadoPor,
                NombreRevocador = permission.UsuarioRevocador?.NombreUsuario,
                EstaActivo = isActive,
                EstaExpirado = isExpired
            };
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
                NumeroAccesos = 0,
                Justificacion = justificacion,
                OtorgadoPor = video.IdAdministrador // El admin del video
            };

            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return ApiResponse<PermissionResponse>.SuccessResponse(
                MapToResponse(permiso, video, usuario), 
                "Solicitud de acceso creada exitosamente");
        }

        public async Task<ApiResponse<bool>> HasAccessAsync(int videoId, int userId)
        {
            // Tipos de permiso válidos: Aprobado (solicitudes aprobadas), Lectura y Temporal (otorgados directamente)
            var validPermissionTypes = new[] { "Aprobado", "Lectura", "Temporal" };
            
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => 
                    p.IdVideo == videoId && 
                    p.IdUsuario == userId && 
                    validPermissionTypes.Contains(p.TipoPermiso) &&
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
            // Tipos de permiso válidos: Aprobado (solicitudes aprobadas), Lectura y Temporal (otorgados directamente)
            var validPermissionTypes = new[] { "Aprobado", "Lectura", "Temporal" };
            
            var permiso = await _context.Permisos
                .Include(p => p.Video)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => 
                    p.IdVideo == videoId && 
                    p.IdUsuario == userId &&
                    validPermissionTypes.Contains(p.TipoPermiso) &&
                    !p.FechaRevocacion.HasValue);

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

        public async Task<ApiResponse<List<PermissionResponse>>> GetPendingRequestsByAdminAsync(int adminId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] GetPendingRequestsByAdminAsync - Admin ID: {adminId}");
                
                // Primero, obtener TODOS los permisos sin filtro para ver qué hay
                var allPermisos = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .ToListAsync();
                
                Console.WriteLine($"[DEBUG] Total de permisos en BD: {allPermisos.Count}");
                foreach (var p in allPermisos)
                {
                    Console.WriteLine($"[DEBUG] Permiso ID:{p.IdPermiso}, Video:{p.IdVideo}, Admin del video:{p.Video?.IdAdministrador}, Usuario:{p.IdUsuario}, Tipo:'{p.TipoPermiso}'");
                }
                
                // Obtener todas las solicitudes pendientes para los videos del administrador
                var pendingRequests = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Where(p => p.Video.IdAdministrador == adminId && p.TipoPermiso == "Pendiente")
                    .OrderByDescending(p => p.FechaOtorgamiento)
                    .ToListAsync();

                Console.WriteLine($"[DEBUG] Se encontraron {pendingRequests.Count} solicitudes pendientes para admin {adminId}");
                
                foreach (var req in pendingRequests)
                {
                    Console.WriteLine($"[DEBUG] Solicitud - ID: {req.IdPermiso}, Video: {req.Video?.TituloVideo}, Usuario: {req.Usuario?.NombreUsuario}, Tipo: {req.TipoPermiso}");
                }

                var responses = pendingRequests.Select(p => new PermissionResponse
                {
                    IdPermiso = p.IdPermiso,
                    IdVideo = p.IdVideo,
                    TituloVideo = p.Video.TituloVideo,
                    IdUsuario = p.IdUsuario,
                    NombreUsuario = p.Usuario.NombreUsuario,
                    TipoPermiso = p.TipoPermiso,
                    FechaOtorgamiento = p.FechaOtorgamiento,
                    Justificacion = p.Justificacion,
                    EstaActivo = false,
                    MensajeEstado = "Pendiente de aprobación"
                }).ToList();

                return ApiResponse<List<PermissionResponse>>.SuccessResponse(
                    responses,
                    $"Se encontraron {responses.Count} solicitudes pendientes"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PermissionResponse>>.ErrorResponse(
                    $"Error al obtener solicitudes pendientes: {ex.Message}"
                );
            }
        }

        public async Task<ApiResponse<PermissionResponse>> ApprovePermissionAsync(
            int permissionId, 
            int adminId, 
            DateTime? fechaExpiracion = null, 
            int? limiteAccesos = null)
        {
            try
            {
                // Obtener el permiso con sus relaciones
                var permiso = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.IdPermiso == permissionId);

                if (permiso == null)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse("Solicitud no encontrada");
                }

                // Verificar que el admin es el dueño del video
                if (permiso.Video.IdAdministrador != adminId)
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse(
                        "Solo el administrador dueño del video puede aprobar solicitudes"
                    );
                }

                // Verificar que el permiso está pendiente
                if (permiso.TipoPermiso != "Pendiente")
                {
                    return ApiResponse<PermissionResponse>.ErrorResponse(
                        "Esta solicitud ya fue procesada"
                    );
                }

                // Aprobar la solicitud
                permiso.TipoPermiso = "Aprobado";
                permiso.FechaExpiracion = fechaExpiracion;
                permiso.MaxAccesos = limiteAccesos;
                permiso.NumeroAccesos = 0; // Reiniciar contador al aprobar

                await _context.SaveChangesAsync();

                var response = MapToResponse(permiso, permiso.Video, permiso.Usuario);

                return ApiResponse<PermissionResponse>.SuccessResponse(
                    response,
                    "Solicitud aprobada correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PermissionResponse>.ErrorResponse(
                    $"Error al aprobar solicitud: {ex.Message}"
                );
            }
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
