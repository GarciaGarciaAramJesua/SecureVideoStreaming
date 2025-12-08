using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserResponse> GetUserByIdAsync(int userId)
        {
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null || !user.Activo)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            return new UserResponse
            {
                IdUsuario = user.IdUsuario,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                TipoUsuario = user.TipoUsuario,
                FechaRegistro = user.FechaRegistro,
                UltimoAcceso = user.UltimoAcceso,
                Activo = user.Activo
            };
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _context.Usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.NombreUsuario)
                .ToListAsync();

            return users.Select(u => new UserResponse
            {
                IdUsuario = u.IdUsuario,
                NombreUsuario = u.NombreUsuario,
                Email = u.Email,
                TipoUsuario = u.TipoUsuario,
                FechaRegistro = u.FechaRegistro,
                UltimoAcceso = u.UltimoAcceso,
                Activo = u.Activo
            });
        }

        public async Task<UserResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null || !user.Activo)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            // Actualizar campos si se proporcionan
            if (!string.IsNullOrWhiteSpace(request.NombreUsuario))
            {
                // Verificar que el nombre de usuario no esté en uso
                var existingUser = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario && u.IdUsuario != userId);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("El nombre de usuario ya está en uso");
                }

                user.NombreUsuario = request.NombreUsuario;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Verificar que el email no esté en uso
                var existingUser = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IdUsuario != userId);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("El email ya está en uso");
                }

                user.Email = request.Email;
            }

            await _context.SaveChangesAsync();

            return new UserResponse
            {
                IdUsuario = user.IdUsuario,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                TipoUsuario = user.TipoUsuario,
                FechaRegistro = user.FechaRegistro,
                UltimoAcceso = user.UltimoAcceso,
                Activo = user.Activo
            };
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            // Soft delete
            user.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ApiResponse<bool>> UpdatePublicKeyAsync(int userId, string publicKey, string fingerprint)
        {
            try
            {
                var user = await _context.Usuarios.FindAsync(userId);

                if (user == null || !user.Activo)
                {
                    return ApiResponse<bool>.ErrorResponse("Usuario no encontrado");
                }

                // Actualizar clave pública y fingerprint
                user.ClavePublicaRSA = publicKey;
                user.PublicKeyFingerprint = fingerprint;

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Clave pública registrada exitosamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error al actualizar clave pública: {ex.Message}");
            }
        }
    }
}
