using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Data.Context;

namespace SecureVideoStreaming.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ApplicationDbContext context, ILogger<DebugController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("pending-permissions")]
        public async Task<IActionResult> GetPendingPermissions()
        {
            try
            {
                // Obtener todos los permisos con TipoPermiso = "Pendiente"
                var pendingPermissions = await _context.Permisos
                    .Include(p => p.Video)
                    .Include(p => p.Usuario)
                    .Where(p => p.TipoPermiso == "Pendiente")
                    .Select(p => new
                    {
                        p.IdPermiso,
                        p.IdVideo,
                        VideoTitulo = p.Video.TituloVideo,
                        VideoAdminId = p.Video.IdAdministrador,
                        p.IdUsuario,
                        UsuarioNombre = p.Usuario.NombreUsuario,
                        p.TipoPermiso,
                        p.FechaOtorgamiento,
                        p.Justificacion,
                        p.OtorgadoPor
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = pendingPermissions.Count,
                    Permissions = pendingPermissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos pendientes");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("admin/{adminId}/videos")]
        public async Task<IActionResult> GetAdminVideos(int adminId)
        {
            try
            {
                var videos = await _context.Videos
                    .Where(v => v.IdAdministrador == adminId)
                    .Select(v => new
                    {
                        v.IdVideo,
                        v.TituloVideo,
                        v.IdAdministrador,
                        v.EstadoProcesamiento
                    })
                    .ToListAsync();

                return Ok(new
                {
                    AdminId = adminId,
                    TotalVideos = videos.Count,
                    Videos = videos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener videos del admin {AdminId}", adminId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUserInfo(string username)
        {
            try
            {
                var user = await _context.Usuarios
                    .Where(u => u.NombreUsuario == username)
                    .Select(u => new
                    {
                        u.IdUsuario,
                        u.NombreUsuario,
                        u.TipoUsuario,
                        u.Activo
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = $"Usuario {username} no encontrado" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del usuario {Username}", username);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
