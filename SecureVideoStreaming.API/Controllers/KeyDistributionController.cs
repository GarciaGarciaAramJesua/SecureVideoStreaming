using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
=======
using SecureVideoStreaming.Models.DTOs.Request;
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeyDistributionController : ControllerBase
    {
<<<<<<< HEAD
        private readonly IKeyDistributionService _keyDistributionService;
        private readonly ILogger<KeyDistributionController> _logger;

        public KeyDistributionController(
            IKeyDistributionService keyDistributionService,
            ILogger<KeyDistributionController> logger)
        {
            _keyDistributionService = keyDistributionService;
=======
        private readonly IPermissionService _permissionService;
        private readonly IKeyDistributionService _keyDistributionService;
        private readonly IVideoStreamingService _videoStreamingService;
        private readonly ILogger<KeyDistributionController> _logger;

        public KeyDistributionController(
            IPermissionService permissionService,
            IKeyDistributionService keyDistributionService,
            IVideoStreamingService videoStreamingService,
            ILogger<KeyDistributionController> logger)
        {
            _permissionService = permissionService;
            _keyDistributionService = keyDistributionService;
            _videoStreamingService = videoStreamingService;
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
            _logger = logger;
        }

        /// <summary>
<<<<<<< HEAD
        /// Solicitar distribución de claves para un video
        /// El usuario debe tener permiso activo para el video
        /// </summary>
        [HttpGet("request/{videoId}")]
        public async Task<IActionResult> RequestKeys(int videoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _keyDistributionService.DistributeKeysAsync(videoId, userId);

                if (!response.Success)
                {
                    _logger.LogWarning(
                        "Solicitud de claves fallida - Video: {VideoId}, Usuario: {UserId}, Motivo: {Message}",
                        videoId, userId, response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation(
                    "Claves distribuidas exitosamente - Video: {VideoId}, Usuario: {UserId}",
                    videoId, userId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al distribuir claves para video {VideoId}", videoId);
                return StatusCode(500, new { message = "Error al distribuir claves" });
            }
        }

        /// <summary>
        /// Validar si un usuario puede solicitar claves para un video
        /// </summary>
        [HttpGet("validate/{videoId}")]
        public async Task<IActionResult> ValidateKeyRequest(int videoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _keyDistributionService.ValidateKeyDistributionAsync(videoId, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar solicitud de claves para video {VideoId}", videoId);
                return StatusCode(500, new { message = "Error al validar solicitud" });
            }
=======
        /// Solicitar acceso a un video
        /// </summary>
        [HttpPost("request-access")]
        public async Task<IActionResult> RequestAccess([FromBody] AccessRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _permissionService.RequestAccessAsync(
                request.VideoId, 
                userId, 
                request.Justificacion);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Obtener mis permisos de acceso
        /// </summary>
        [HttpGet("my-permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _permissionService.GetMyPermissionsAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Obtener paquete de claves para video (requiere permiso aprobado)
        /// </summary>
        [HttpPost("get-key-package")]
        public async Task<IActionResult> GetKeyPackage([FromBody] KeyPackageRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _keyDistributionService.GetKeyPackageAsync(
                request.VideoId,
                userId,
                request.UserPublicKey);

            if (!result.Success)
                return StatusCode(403, result);

            _logger.LogInformation(
                "Usuario {UserId} obtuvo paquete de claves para video {VideoId}", 
                userId, 
                request.VideoId);

            return Ok(result);
        }

        /// <summary>
        /// Aprobar solicitud de acceso (solo administrador dueño del video)
        /// </summary>
        [HttpPost("approve/{permisoId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ApproveAccess(
            int permisoId,
            [FromBody] ApproveAccessRequest request)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (adminId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _permissionService.ApproveAccessAsync(
                permisoId,
                adminId,
                request.MaxAccesos,
                request.FechaExpiracion);

            if (!result.Success)
                return BadRequest(result);

            _logger.LogInformation(
                "Admin {AdminId} aprobó permiso {PermisoId}", 
                adminId, 
                permisoId);

            return Ok(result);
        }

        /// <summary>
        /// Revocar acceso a video (solo administrador dueño)
        /// </summary>
        [HttpDelete("revoke/{permisoId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RevokeAccess(int permisoId)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (adminId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _permissionService.RevokeAccessAsync(permisoId, adminId);

            if (!result.Success)
                return BadRequest(result);

            _logger.LogInformation(
                "Admin {AdminId} revocó permiso {PermisoId}", 
                adminId, 
                permisoId);

            return Ok(result);
        }

        /// <summary>
        /// Listar permisos de un video (solo administrador dueño)
        /// </summary>
        [HttpGet("video/{videoId}/permissions")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetVideoPermissions(int videoId)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (adminId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var result = await _permissionService.GetVideoPermissionsAsync(videoId, adminId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
>>>>>>> 15998941304142dc5144d5f74b8ee48b369d7458
        }
    }
}
