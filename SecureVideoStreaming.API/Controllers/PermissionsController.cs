using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(
            IPermissionService permissionService,
            ILogger<PermissionsController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Otorgar permiso de acceso a un video (solo administradores)
        /// </summary>
        [HttpPost("grant")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GrantPermission([FromBody] GrantPermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verificar que el administrador autenticado es quien otorga
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                if (request.OtorgadoPor != userId)
                {
                    return Forbid("Solo puedes otorgar permisos de tus propios videos");
                }

                var response = await _permissionService.GrantPermissionAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al otorgar permiso");
                return StatusCode(500, new { message = "Error al otorgar permiso" });
            }
        }

        /// <summary>
        /// Revocar permiso de acceso (solo administradores)
        /// </summary>
        [HttpDelete("{permissionId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RevokePermission(int permissionId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _permissionService.RevokePermissionAsync(permissionId, userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar permiso {PermissionId}", permissionId);
                return StatusCode(500, new { message = "Error al revocar permiso" });
            }
        }

        /// <summary>
        /// Verificar si un usuario tiene permiso para un video
        /// </summary>
        [HttpGet("check")]
        public async Task<IActionResult> CheckPermission([FromQuery] int videoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _permissionService.CheckPermissionAsync(videoId, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar permiso");
                return StatusCode(500, new { message = "Error al verificar permiso" });
            }
        }

        /// <summary>
        /// Obtener permisos de un video específico (solo el admin dueño)
        /// </summary>
        [HttpGet("video/{videoId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetPermissionsByVideo(int videoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _permissionService.GetPermissionsByVideoAsync(videoId, userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del video {VideoId}", videoId);
                return StatusCode(500, new { message = "Error al obtener permisos" });
            }
        }

        /// <summary>
        /// Obtener todos los permisos del usuario autenticado (videos a los que tiene acceso)
        /// </summary>
        [HttpGet("my-permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _permissionService.GetPermissionsByUserAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del usuario");
                return StatusCode(500, new { message = "Error al obtener permisos" });
            }
        }

        /// <summary>
        /// Extender fecha de expiración de un permiso (solo administradores)
        /// </summary>
        [HttpPut("{permissionId}/extend")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ExtendPermission(int permissionId, [FromBody] DateTime newExpiration)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _permissionService.ExtendPermissionAsync(permissionId, newExpiration, userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extender permiso {PermissionId}", permissionId);
                return StatusCode(500, new { message = "Error al extender permiso" });
            }
        }

        /// <summary>
        /// Obtener detalles de un permiso específico
        /// </summary>
        [HttpGet("{permissionId}")]
        public async Task<IActionResult> GetPermissionById(int permissionId)
        {
            try
            {
                var response = await _permissionService.GetPermissionByIdAsync(permissionId);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permiso {PermissionId}", permissionId);
                return StatusCode(500, new { message = "Error al obtener permiso" });
            }
        }
    }
}
