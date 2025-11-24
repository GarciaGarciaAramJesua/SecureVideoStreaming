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
        /// Obtener mis videos (solo administradores)
        /// </summary>
        [HttpGet("my-videos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetMyVideos()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var videos = await _videoService.GetVideosByAdminAsync(userId);
                return Ok(videos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mis videos");
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVideo([FromForm] UploadVideoFormRequest request)
        {
            try
            {
                if (request.VideoFile == null || request.VideoFile.Length == 0)
                {
                    return BadRequest(new { message = "El archivo de video es requerido" });
                }

                if (string.IsNullOrWhiteSpace(request.Titulo))
                {
                    return BadRequest(new { message = "El título es requerido" });
                }

                // Obtener ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var uploadRequest = new UploadVideoRequest
                {
                    IdAdministrador = userId,
                    NombreArchivo = request.VideoFile.FileName,
                    Descripcion = request.Descripcion
                };

                using var stream = request.VideoFile.OpenReadStream();
                var response = await _videoService.UploadVideoAsync(uploadRequest, stream);

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
        /// Verificar integridad de un video (solo el administrador dueño)
        /// </summary>
        [HttpPost("{id}/verify-integrity")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> VerifyVideoIntegrity(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _videoService.VerifyVideoIntegrityAsync(id, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
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
                _logger.LogError(ex, "Error al verificar integridad del video {VideoId}", id);
                return StatusCode(500, new { message = "Error al verificar integridad" });
            }
        }

        /// <summary>
        /// Actualizar metadata de un video (solo el administrador dueño)
        /// </summary>
        [HttpPut("{id}/metadata")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateVideoMetadata(int id, [FromBody] UpdateVideoMetadataRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _videoService.UpdateVideoMetadataAsync(id, request, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
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
                _logger.LogError(ex, "Error al actualizar metadata del video {VideoId}", id);
                return StatusCode(500, new { message = "Error al actualizar metadata" });
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
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
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
