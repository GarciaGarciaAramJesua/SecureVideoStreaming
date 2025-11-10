using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.API.Pages
{
    public class UploadVideoModel : PageModel
    {
        private readonly IVideoService _videoService;

        [BindProperty]
        [Required(ErrorMessage = "Debe seleccionar un archivo de video")]
        public IFormFile VideoFile { get; set; } = null!;

        [BindProperty]
        public string? Description { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public UploadVideoModel(IVideoService videoService)
        {
            _videoService = videoService;
        }

        public IActionResult OnGet()
        {
            // Verificar si el usuario está logueado
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            // Verificar si es administrador
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Administrador")
            {
                return RedirectToPage("/Home");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Administrador")
            {
                ErrorMessage = "Solo los administradores pueden subir videos";
                return Page();
            }

            if (!ModelState.IsValid || VideoFile == null)
            {
                ErrorMessage = "Debe seleccionar un archivo de video válido";
                return Page();
            }

            // Validar tamaño (máximo 500 MB)
            if (VideoFile.Length > 500 * 1024 * 1024)
            {
                ErrorMessage = "El archivo es demasiado grande. Tamaño máximo: 500 MB";
                return Page();
            }

            // Validar extensión
            var allowedExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
            var extension = Path.GetExtension(VideoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ErrorMessage = $"Formato de video no soportado. Use: {string.Join(", ", allowedExtensions)}";
                return Page();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    ErrorMessage = "Usuario no válido";
                    return Page();
                }

                // Convertir IFormFile a Stream
                using var stream = VideoFile.OpenReadStream();
                
                var request = new UploadVideoRequest
                {
                    NombreArchivo = VideoFile.FileName,
                    IdAdministrador = userId.Value,
                    Descripcion = Description
                };

                var response = await _videoService.UploadVideoAsync(request, stream);

                if (response.Success)
                {
                    SuccessMessage = "Video subido y cifrado correctamente";
                    // Redirigir al home después de 2 segundos
                    await Task.Delay(2000);
                    return RedirectToPage("/Home");
                }
                else
                {
                    ErrorMessage = response.Message ?? "Error al subir el video";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al subir video: {ex.Message}";
                return Page();
            }
        }
    }
}
