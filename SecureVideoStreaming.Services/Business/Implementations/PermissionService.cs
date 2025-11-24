using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Business.Interfaces;

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

        public async Task<ApiResponse<bool>> IncrementAccessCountAsync(int permissionId)
        {
            try
            {
                var permission = await _context.Permisos.FindAsync(permissionId);
                if (permission == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Permiso no encontrado");
                }

                permission.NumeroAccesos++;
                permission.UltimoAcceso = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error al actualizar contador: {ex.Message}");
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
    }
}
