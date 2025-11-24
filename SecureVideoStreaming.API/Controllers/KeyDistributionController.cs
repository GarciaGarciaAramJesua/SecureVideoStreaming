using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeyDistributionController : ControllerBase
    {
        private readonly IKeyDistributionService _keyDistributionService;
        private readonly ILogger<KeyDistributionController> _logger;

        public KeyDistributionController(
            IKeyDistributionService keyDistributionService,
            ILogger<KeyDistributionController> logger)
        {
            _keyDistributionService = keyDistributionService;
            _logger = logger;
        }

        /// <summary>
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
        }
    }
}
