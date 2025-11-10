using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VideosController : ControllerBase
    {
        private readonly IVideoService _videoService;
        private readonly ILogger<VideosController> _logger;

        public VideosController(IVideoService videoService, ILogger<VideosController> logger)
        {
            _videoService = videoService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los videos disponibles
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllVideos()
        {
            try
            {
                var videos = await _videoService.GetAllVideosAsync();
                return Ok(videos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener videos");
                return StatusCode(500, new { message = "Error al obtener videos" });
            }
        }

        /// <summary>
        /// Obtener videos de un administrador específico
        /// </summary>
        [HttpGet("admin/{adminId}")]
        public async Task<IActionResult> GetVideosByAdmin(int adminId)
        {
            try
            {
                var videos = await _videoService.GetVideosByAdminAsync(adminId);
                return Ok(videos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener videos del admin {AdminId}", adminId);
                return StatusCode(500, new { message = "Error al obtener videos" });
            }
        }

        /// <summary>
        /// Obtener video por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideoById(int id)
        {
            try
            {
                var video = await _videoService.GetVideoByIdAsync(id);
                return Ok(video);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener video {VideoId}", id);
                return StatusCode(500, new { message = "Error al obtener video" });
            }
        }

        /// <summary>
        /// Subir un nuevo video (solo administradores)
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "Administrador")]
        [RequestSizeLimit(500_000_000)] // 500 MB
        public async Task<IActionResult> UploadVideo([FromForm] string titulo, [FromForm] string? descripcion, [FromForm] IFormFile videoFile)
        {
            try
            {
                if (videoFile == null || videoFile.Length == 0)
                {
                    return BadRequest(new { message = "El archivo de video es requerido" });
                }

                if (string.IsNullOrWhiteSpace(titulo))
                {
                    return BadRequest(new { message = "El título es requerido" });
                }

                // Obtener ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var request = new SecureVideoStreaming.Models.DTOs.Request.UploadVideoRequest
                {
                    IdAdministrador = userId,
                    NombreArchivo = videoFile.FileName,
                    Descripcion = descripcion
                };

                using var stream = videoFile.OpenReadStream();
                var response = await _videoService.UploadVideoAsync(request, stream);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir video");
                return StatusCode(500, new { message = "Error al subir video" });
            }
        }

        /// <summary>
        /// Eliminar video (solo el administrador dueño)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            try
            {
                // Obtener ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _videoService.DeleteVideoAsync(id, userId);
                return Ok(new { message = "Video eliminado exitosamente", success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar video {VideoId}", id);
                return StatusCode(500, new { message = "Error al eliminar video" });
            }
        }
    }
}
