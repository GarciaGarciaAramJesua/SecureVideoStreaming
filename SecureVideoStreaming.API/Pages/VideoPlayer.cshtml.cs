using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class VideoPlayerModel : PageModel
    {
        private readonly IVideoService _videoService;
        private readonly IPermissionService _permissionService;
        private readonly IKeyDistributionService _keyDistributionService;
        private readonly ILogger<VideoPlayerModel> _logger;

        public VideoResponse? Video { get; set; }
        public PermissionResponse? Permission { get; set; }
        
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public VideoPlayerModel(
            IVideoService videoService,
            IPermissionService permissionService,
            IKeyDistributionService keyDistributionService,
            ILogger<VideoPlayerModel> logger)
        {
            _videoService = videoService;
            _permissionService = permissionService;
            _keyDistributionService = keyDistributionService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "Debes iniciar sesión para ver videos";
                return RedirectToPage("/Login");
            }

            Username = username;
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Usuario no válido";
                return RedirectToPage("/Login");
            }

            UserId = userId.Value;

            if (Id <= 0)
            {
                TempData["ErrorMessage"] = "ID de video no válido";
                return RedirectToPage("/Home");
            }

            try
            {
                // Cargar información del video
                var videoResponse = await _videoService.GetVideoByIdAsync(Id);
                if (!videoResponse.Success || videoResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "Video no encontrado";
                    return RedirectToPage("/VideoGrid");
                }

                Video = videoResponse.Data;

                // Verificar si el usuario es el administrador dueño del video
                var userType = HttpContext.Session.GetString("UserType");
                bool isAdmin = userType == "Administrador";
                bool isOwner = Video.IdAdministrador == UserId;

                if (isAdmin && isOwner)
                {
                    // El administrador dueño tiene acceso total sin verificar permisos
                    HasAccess = true;
                    _logger.LogInformation("Administrador {UserId} accediendo a su propio video {VideoId}", UserId, Id);
                }
                else
                {
                    // Verificar si el usuario tiene permiso
                    var accessResponse = await _permissionService.HasAccessAsync(Id, UserId);
                    if (accessResponse.Success && accessResponse.Data)
                    {
                        HasAccess = true;

                        // Obtener información del permiso
                        var permissionResponse = await _permissionService.GetPermissionAsync(Id, UserId);
                        if (permissionResponse.Success && permissionResponse.Data != null)
                        {
                            Permission = permissionResponse.Data;
                        }

                        // Registrar acceso
                        await _permissionService.RegisterAccessAsync(Id, UserId, true);
                    }
                    else
                    {
                        HasAccess = false;
                        ErrorMessage = "No tienes permiso para ver este video";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar video {VideoId} para usuario {UserId}", Id, UserId);
                ErrorMessage = "Error al cargar el video";
            }

            return Page();
        }

        /// <summary>
        /// Handler para obtener el key package (AJAX con Session)
        /// </summary>
        public async Task<IActionResult> OnPostGetKeyPackageAsync([FromBody] KeyPackageRequest request)
        {
            try
            {
                // Verificar autenticación mediante sesión
                var userIdSession = HttpContext.Session.GetInt32("UserId");
                if (!userIdSession.HasValue)
                {
                    _logger.LogWarning("Intento de obtener key package sin autenticación");
                    return new JsonResult(new
                    {
                        success = false,
                        message = "No autenticado"
                    })
                    { StatusCode = 401 };
                }

                int userId = userIdSession.Value;
                var userType = HttpContext.Session.GetString("UserType");
                bool isAdmin = userType == "Administrador";

                _logger.LogInformation(
                    "Usuario {UserId} solicitando key package para video {VideoId}",
                    userId,
                    request.VideoId
                );

                // Verificar si es el administrador dueño del video
                var videoResponse = await _videoService.GetVideoByIdAsync(request.VideoId);
                bool isOwner = videoResponse.Success && videoResponse.Data != null && videoResponse.Data.IdAdministrador == userId;

                if (isAdmin && isOwner)
                {
                    // El administrador puede acceder a sus propios videos sin restricciones
                    _logger.LogInformation(
                        "Administrador {UserId} accediendo a key package de su propio video {VideoId}",
                        userId,
                        request.VideoId
                    );
                }

                // Obtener key package usando el servicio
                var result = await _keyDistributionService.GetKeyPackageAsync(
                    request.VideoId,
                    userId,
                    request.UserPublicKey
                );

                if (!result.Success)
                {
                    // Si es administrador dueño y falló por permisos, permitir de todas formas
                    if (isAdmin && isOwner && result.Message != null && result.Message.Contains("permiso"))
                    {
                        _logger.LogWarning(
                            "Administrador dueño intentó acceder pero falló verificación de permisos. Esto no debería ocurrir."
                        );
                    }
                    
                    _logger.LogWarning(
                        "No se pudo obtener key package: {Message}",
                        result.Message
                    );
                    return new JsonResult(result)
                    { StatusCode = 403 };
                }

                _logger.LogInformation(
                    "Key package entregado exitosamente a usuario {UserId} para video {VideoId}",
                    userId,
                    request.VideoId
                );

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener key package para video {VideoId}",
                    request?.VideoId
                );
                return new JsonResult(new
                {
                    success = false,
                    message = "Error al obtener paquete de claves"
                })
                { StatusCode = 500 };
            }
        }
    }

    /// <summary>
    /// Request DTO para key package
    /// </summary>
    public class KeyPackageRequest
    {
        public int VideoId { get; set; }
        public string UserPublicKey { get; set; } = string.Empty;
    }
}
