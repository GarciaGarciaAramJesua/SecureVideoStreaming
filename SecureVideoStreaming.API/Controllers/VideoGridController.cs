using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VideoGridController : ControllerBase
    {
        private readonly IVideoGridService _videoGridService;
        private readonly ILogger<VideoGridController> _logger;

        public VideoGridController(
            IVideoGridService videoGridService,
            ILogger<VideoGridController> logger)
        {
            _videoGridService = videoGridService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener grid de videos disponibles para el usuario autenticado
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVideoGrid()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _videoGridService.GetVideoGridForUserAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grid de videos");
                return StatusCode(500, new { message = "Error al obtener grid de videos" });
            }
        }

        /// <summary>
        /// Obtener grid de videos con filtros
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> GetVideoGridWithFilters(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? administrador = null,
            [FromQuery] bool? soloConPermiso = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _videoGridService.GetVideoGridWithFiltersAsync(
                    userId, 
                    searchTerm, 
                    administrador, 
                    soloConPermiso);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grid filtrado");
                return StatusCode(500, new { message = "Error al obtener grid filtrado" });
            }
        }

        /// <summary>
        /// Obtener detalles de un video para el grid
        /// </summary>
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetVideoGridItem(int videoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var response = await _videoGridService.GetVideoGridItemAsync(videoId, userId);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener item del grid {VideoId}", videoId);
                return StatusCode(500, new { message = "Error al obtener video" });
            }
        }
    }
}
