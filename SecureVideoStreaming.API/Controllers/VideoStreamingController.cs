using Microsoft.AspNetCore.Mvc;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoStreamingController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<VideoStreamingController> _logger;

    public VideoStreamingController(
        IVideoService videoService,
        IPermissionService permissionService,
        ILogger<VideoStreamingController> logger)
    {
        _videoService = videoService;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el video cifrado para un usuario específico
    /// </summary>
    [HttpGet("stream/{videoId}")]
    public async Task<IActionResult> GetEncryptedVideo(int videoId, [FromQuery] int userId)
    {
        try
        {
            _logger.LogInformation(
                "Solicitud de video cifrado - VideoId: {VideoId}, UserId: {UserId}",
                videoId,
                userId
            );

            // Verificar que el usuario tenga permiso para ver el video
            // IMPORTANTE: Orden correcto de parámetros: (videoId, userId)
            var hasAccessResponse = await _permissionService.HasAccessAsync(videoId, userId);
            
            _logger.LogInformation(
                "Verificación de acceso - Success: {Success}, HasAccess: {HasAccess}",
                hasAccessResponse.Success,
                hasAccessResponse.Data
            );

            if (!hasAccessResponse.Success || hasAccessResponse.Data == false)
            {
                _logger.LogWarning(
                    "Acceso denegado - VideoId: {VideoId}, UserId: {UserId}",
                    videoId,
                    userId
                );
                return Ok(ApiResponse<object>.ErrorResponse("No tienes permiso para acceder a este video"));
            }

            // Obtener el video cifrado
            var videoResponse = await _videoService.GetEncryptedVideoDataAsync(videoId);
            if (!videoResponse.Success || videoResponse.Data == null)
            {
                return Ok(ApiResponse<object>.ErrorResponse(videoResponse.Message ?? "No se pudo obtener el video"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(videoResponse.Data, "Video cifrado obtenido correctamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener video cifrado {VideoId}", videoId);
            return Ok(ApiResponse<object>.ErrorResponse("Error al obtener el video cifrado"));
        }
    }

    /// <summary>
    /// Registra un acceso a un video
    /// </summary>
    [HttpPost("register-access")]
    public async Task<IActionResult> RegisterAccess([FromBody] RegisterAccessRequest request)
    {
        try
        {
            if (request == null || request.VideoId <= 0 || request.UserId <= 0)
            {
                return Ok(ApiResponse<object>.ErrorResponse("Datos de solicitud inválidos"));
            }

            // Verificar permiso - IMPORTANTE: Orden correcto (videoId, userId)
            var hasAccessResponse = await _permissionService.HasAccessAsync(request.VideoId, request.UserId);
            if (!hasAccessResponse.Success || hasAccessResponse.Data == false)
            {
                return Ok(ApiResponse<object>.ErrorResponse("No tienes permiso para acceder a este video"));
            }

            // Registrar acceso - IMPORTANTE: Orden correcto (videoId, userId)
            await _permissionService.RegisterAccessAsync(request.VideoId, request.UserId, true, null);
            
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Acceso registrado correctamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar acceso");
            return Ok(ApiResponse<object>.ErrorResponse("Error al registrar el acceso"));
        }
    }
}

public class RegisterAccessRequest
{
    public int VideoId { get; set; }
    public int UserId { get; set; }
}
