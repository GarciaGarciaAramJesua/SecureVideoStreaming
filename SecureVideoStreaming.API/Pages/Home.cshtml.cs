using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class HomeModel : PageModel
    {
        private readonly IVideoService _videoService;
        private readonly IPermissionService _permissionService;

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public int UserId { get; set; }
        public List<VideoListResponse> Videos { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Estadísticas para administradores
        public int TotalVideos { get; set; }
        public int TotalPermisosActivos { get; set; }
        public long AlmacenamientoUsado { get; set; }
        public int VideosSubidosHoy { get; set; }

        // Estadísticas para consumidores
        public int PermisosDisponibles { get; set; }
        public int PermisosExpirados { get; set; }

        public HomeModel(IVideoService videoService, IPermissionService permissionService)
        {
            _videoService = videoService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verificar si el usuario está logueado
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            Username = username;
            UserType = HttpContext.Session.GetString("UserType") ?? "";
            IsAdmin = UserType == "Administrador";
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (IsAdmin)
                {
                    // Administradores obtienen solo sus propios videos
                    var response = await _videoService.GetVideosByAdminAsync(UserId);
                    if (response.Success && response.Data != null)
                    {
                        Videos = response.Data;
                    }
                    else
                    {
                        ErrorMessage = response.Message ?? "No se pudieron cargar los videos";
                    }
                }
                else
                {
                    // Consumidores solo ven videos a los que tienen acceso
                    var allVideosResponse = await _videoService.GetAllVideosAsync();
                    if (allVideosResponse.Success && allVideosResponse.Data != null)
                    {
                        var videosConAcceso = new List<VideoListResponse>();
                        foreach (var video in allVideosResponse.Data)
                        {
                            var accessResponse = await _permissionService.CheckPermissionAsync(video.IdVideo, UserId);
                            if (accessResponse.Success && accessResponse.Data)
                            {
                                videosConAcceso.Add(video);
                            }
                        }
                        Videos = videosConAcceso;
                    }
                    else
                    {
                        ErrorMessage = allVideosResponse.Message ?? "No se pudieron cargar los videos";
                    }
                }

                // Cargar estadísticas según el tipo de usuario
                if (IsAdmin)
                {
                    await LoadAdminStatisticsAsync();
                }
                else
                {
                    await LoadConsumerStatisticsAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar videos: {ex.Message}";
            }

            return Page();
        }

        private async Task LoadAdminStatisticsAsync()
        {
            try
            {
                // Videos del administrador actual
                var myVideos = Videos;
                TotalVideos = myVideos.Count();
                AlmacenamientoUsado = myVideos.Sum(v => v.TamañoArchivo);
                VideosSubidosHoy = myVideos.Count(v => v.FechaSubida.Date == DateTime.UtcNow.Date);

                // Total de permisos activos otorgados por este admin (aproximado)
                TotalPermisosActivos = 0;
                foreach (var video in myVideos)
                {
                    var permissionsResponse = await _permissionService.GetPermissionsByVideoAsync(video.IdVideo, UserId);
                    if (permissionsResponse.Success && permissionsResponse.Data != null)
                    {
                        TotalPermisosActivos += permissionsResponse.Data.Count(p => p.EstaActivo);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorar errores en estadísticas
                Console.WriteLine($"Error al cargar estadísticas de admin: {ex.Message}");
            }
        }

        private async Task LoadConsumerStatisticsAsync()
        {
            try
            {
                var permissionsResponse = await _permissionService.GetPermissionsByUserAsync(UserId);
                if (permissionsResponse.Success && permissionsResponse.Data != null)
                {
                    PermisosDisponibles = permissionsResponse.Data.Count(p => p.EstaActivo);
                    PermisosExpirados = permissionsResponse.Data.Count(p => !p.EstaActivo);
                }
            }
            catch (Exception ex)
            {
                // Ignorar errores en estadísticas
                Console.WriteLine($"Error al cargar estadísticas de consumidor: {ex.Message}");
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
                ErrorMessage = "Usuario no válido";
                return RedirectToPage("/Home");
            }

            try
            {
                var response = await _videoService.DeleteVideoAsync(id, userId.Value);
                if (response.Success)
                {
                    SuccessMessage = "Video eliminado correctamente";
                }
                else
                {
                    ErrorMessage = response.Message ?? "No se pudo eliminar el video";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar video: {ex.Message}";
            }

            return RedirectToPage("/Home");
        }
    }
}
