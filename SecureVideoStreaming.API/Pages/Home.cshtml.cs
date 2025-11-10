using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class HomeModel : PageModel
    {
        private readonly IVideoService _videoService;

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public List<VideoListResponse> Videos { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public HomeModel(IVideoService videoService)
        {
            _videoService = videoService;
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

            try
            {
                // Obtener todos los videos
                var response = await _videoService.GetAllVideosAsync();
                if (response.Success && response.Data != null)
                {
                    Videos = response.Data;
                }
                else
                {
                    ErrorMessage = response.Message ?? "No se pudieron cargar los videos";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar videos: {ex.Message}";
            }

            return Page();
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
