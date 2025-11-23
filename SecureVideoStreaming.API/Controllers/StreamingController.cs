using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.Security.Claims;

namespace SecureVideoStreaming.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StreamingController : ControllerBase
    {
        private readonly IVideoStreamingService _videoStreamingService;
        private readonly IPermissionService _permissionService;
        private readonly IKeyDistributionService _keyDistributionService;
        private readonly ILogger<StreamingController> _logger;

        public StreamingController(
            IVideoStreamingService videoStreamingService,
            IPermissionService permissionService,
            IKeyDistributionService keyDistributionService,
            ILogger<StreamingController> logger)
        {
            _videoStreamingService = videoStreamingService;
            _permissionService = permissionService;
            _keyDistributionService = keyDistributionService;
            _logger = logger;
        }

        /// <summary>
        /// Stream de video cifrado con soporte para HTTP Range requests (chunked download)
        /// </summary>
        [HttpGet("video/{videoId}")]
        public async Task<IActionResult> StreamVideo(
            int videoId,
            [FromHeader(Name = "Range")] string? rangeHeader = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized(new { message = "Usuario no autenticado" });

            try
            {
                // 1. Validar permiso de acceso
                var hasAccessResponse = await _permissionService.HasAccessAsync(videoId, userId);
                if (!hasAccessResponse.Success || !hasAccessResponse.Data)
                {
                    await _permissionService.RegisterAccessAsync(
                        videoId, 
                        userId, 
                        false, 
                        "Acceso denegado para streaming");
                    
                    return StatusCode(403, new { message = "No tiene permiso para acceder a este video" });
                }

                // 2. Obtener ruta del video
                var videoPath = Path.Combine("Storage", "Videos", $"{videoId}.encrypted");
                if (!await _videoStreamingService.ValidateVideoFileAsync(videoPath))
                {
                    _logger.LogError("Video {VideoId} no encontrado en ruta {Path}", videoId, videoPath);
                    return NotFound(new { message = "Video no encontrado" });
                }

                // 3. Obtener información del archivo
                var (fileSize, contentType) = await _videoStreamingService.GetVideoInfoAsync(videoPath);

                // 4. Procesar Range request si existe
                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    return await ProcessRangeRequest(videoId, userId, videoPath, rangeHeader, fileSize);
                }

                // 5. Sin Range request: devolver archivo completo
                var stream = System.IO.File.OpenRead(videoPath);
                
                await _permissionService.RegisterAccessAsync(videoId, userId, true);
                
                _logger.LogInformation(
                    "Usuario {UserId} inició streaming completo de video {VideoId}", 
                    userId, 
                    videoId);

                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming video {VideoId} para usuario {UserId}", videoId, userId);
                await _permissionService.RegisterAccessAsync(videoId, userId, false, ex.Message);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private async Task<IActionResult> ProcessRangeRequest(
            int videoId,
            int userId,
            string videoPath,
            string rangeHeader,
            long fileSize)
        {
            try
            {
                // Parsear Range header: "bytes=0-1023" o "bytes=1024-"
                var range = rangeHeader.Replace("bytes=", "").Split('-');
                var rangeStart = long.Parse(range[0]);
                var rangeEnd = range.Length > 1 && !string.IsNullOrEmpty(range[1]) 
                    ? long.Parse(range[1]) 
                    : (long?)null;

                // Obtener chunk
                var (stream, totalSize, start, end) = await _videoStreamingService.GetVideoChunkAsync(
                    videoPath, 
                    rangeStart, 
                    rangeEnd);

                // Calcular Content-Length
                var contentLength = end - start + 1;

                // Configurar headers para 206 Partial Content
                Response.StatusCode = 206;
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Content-Range"] = $"bytes {start}-{end}/{totalSize}";
                Response.ContentLength = contentLength;
                Response.ContentType = "application/octet-stream";

                await _permissionService.RegisterAccessAsync(videoId, userId, true);

                _logger.LogInformation(
                    "Usuario {UserId} streaming chunk {Start}-{End} de video {VideoId}", 
                    userId, 
                    start, 
                    end, 
                    videoId);

                return new FileStreamResult(stream, "application/octet-stream")
                {
                    EnableRangeProcessing = false // Ya procesamos manualmente
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Rango inválido en request de usuario {UserId}", userId);
                return StatusCode(416, new { message = "Rango solicitado no satisfactorio" }); // 416 Range Not Satisfiable
            }
        }
    }
}
