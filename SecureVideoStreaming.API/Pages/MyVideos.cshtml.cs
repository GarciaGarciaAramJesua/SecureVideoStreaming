using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class MyVideosModel : PageModel
    {
        private readonly IVideoService _videoService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<MyVideosModel> _logger;

        public List<VideoListResponse> MyVideos { get; set; } = new();
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Estadísticas
        public int TotalVideos { get; set; }
        public string TotalAlmacenamiento { get; set; } = "0 B";
        public int TotalPermisos { get; set; }

        public MyVideosModel(
            IVideoService videoService,
            IPermissionService permissionService,
            ILogger<MyVideosModel> logger)
        {
            _videoService = videoService;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "Debes iniciar sesión";
                return RedirectToPage("/Login");
            }

            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Administrador")
            {
                TempData["ErrorMessage"] = "Solo los administradores pueden acceder a esta página";
                return RedirectToPage("/Home");
            }

            Username = username;
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                // Obtener todos los videos del administrador actual
                var response = await _videoService.GetVideosByAdminAsync(UserId);
                
                if (response.Success && response.Data != null)
                {
                    MyVideos = response.Data;
                    
                    // Calcular estadísticas
                    TotalVideos = MyVideos.Count;
                    TotalAlmacenamiento = FormatBytes(MyVideos.Sum(v => v.TamañoArchivo));
                    
                    // Obtener conteo de permisos por video
                    foreach (var video in MyVideos)
                    {
                        var permissionsResponse = await _permissionService.GetPermissionsByVideoAsync(video.IdVideo, UserId);
                        if (permissionsResponse.Success && permissionsResponse.Data != null)
                        {
                            TotalPermisos += permissionsResponse.Data.Count(p => p.EstaActivo);
                        }
                    }
                }
                else
                {
                    ErrorMessage = response.Message ?? "No se pudieron cargar los videos";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar videos del administrador {UserId}", UserId);
                ErrorMessage = "Error al cargar los videos";
            }

            return Page();
        }

        public async Task<IActionResult> OnGetVideoPermissionsAsync(int videoId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "No autenticado" });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Usuario no válido" });
            }

            try
            {
                var response = await _permissionService.GetPermissionsByVideoAsync(videoId, userId.Value);
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del video {VideoId}", videoId);
                return new JsonResult(new { success = false, message = "Error al obtener permisos" });
            }
        }

        public async Task<IActionResult> OnPostDeleteVideoAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Usuario no válido";
                return RedirectToPage("/MyVideos");
            }

            try
            {
                var response = await _videoService.DeleteVideoAsync(id, userId.Value);
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Video eliminado correctamente";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo eliminar el video";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar video {VideoId}", id);
                TempData["ErrorMessage"] = "Error al eliminar el video";
            }

            return RedirectToPage("/MyVideos");
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            else
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
