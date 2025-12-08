using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.Security.Cryptography;

namespace SecureVideoStreaming.API.Pages
{
    public class IntegrityCheckModel : PageModel
    {
        private readonly IVideoService _videoService;
        private readonly IChaCha20Poly1305Service _chaChaService;
        private readonly ILogger<IntegrityCheckModel> _logger;
        private readonly IConfiguration _configuration;

        public List<VideoListResponse> AdminVideos { get; set; } = new();
        public List<IntegrityCheckResult> CheckResults { get; set; } = new();
        
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsChecking { get; set; }

        [BindProperty]
        public int SelectedVideoId { get; set; }

        [BindProperty]
        public bool CheckAll { get; set; }

        public IntegrityCheckModel(
            IVideoService videoService,
            IChaCha20Poly1305Service chaChaService,
            ILogger<IntegrityCheckModel> logger,
            IConfiguration configuration)
        {
            _videoService = videoService;
            _chaChaService = chaChaService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "Debes iniciar sesión";
                return RedirectToPage("/Login");
            }

            Username = username;
            var userType = HttpContext.Session.GetString("UserType");

            if (userType != "Administrador")
            {
                TempData["ErrorMessage"] = "No tienes permisos para acceder a esta página";
                return RedirectToPage("/Home");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Usuario no válido";
                return RedirectToPage("/Login");
            }

            UserId = userId.Value;

            try
            {
                // Cargar videos del administrador
                var videosResponse = await _videoService.GetVideosByAdminAsync(UserId);
                if (videosResponse.Success && videosResponse.Data != null)
                {
                    AdminVideos = videosResponse.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar videos del administrador");
                ErrorMessage = "Error al cargar los videos";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCheckIntegrityAsync()
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
                return RedirectToPage("/IntegrityCheck");
            }

            UserId = userId.Value;
            IsChecking = true;

            try
            {
                // Cargar videos del administrador primero
                var videosResponse = await _videoService.GetVideosByAdminAsync(UserId);
                if (videosResponse.Success && videosResponse.Data != null)
                {
                    AdminVideos = videosResponse.Data;
                }

                List<VideoListResponse> videosToCheck = new();

                if (CheckAll)   
                {
                    videosToCheck = AdminVideos;
                }
                else if (SelectedVideoId > 0)
                {
                    var selectedVideo = AdminVideos.FirstOrDefault(v => v.IdVideo == SelectedVideoId);
                    if (selectedVideo != null)
                    {
                        videosToCheck.Add(selectedVideo);
                    }
                }

                if (!videosToCheck.Any())
                {
                    TempData["ErrorMessage"] = "No se seleccionaron videos para verificar";
                    return RedirectToPage("/IntegrityCheck");
                }

                // Verificar cada video
                foreach (var video in videosToCheck)
                {
                    var result = await CheckVideoIntegrityAsync(video);
                    CheckResults.Add(result);
                }

                var failedChecks = CheckResults.Count(r => !r.IsValid);
                if (failedChecks == 0)
                {
                    TempData["SuccessMessage"] = $"Verificación completada: Todos los videos ({CheckResults.Count}) están íntegros";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Verificación completada: {failedChecks} de {CheckResults.Count} videos tienen problemas de integridad";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar integridad de videos");
                TempData["ErrorMessage"] = "Error al verificar la integridad";
            }

            return Page();
        }

        private async Task<IntegrityCheckResult> CheckVideoIntegrityAsync(VideoListResponse video)
        {
            var result = new IntegrityCheckResult
            {
                VideoId = video.IdVideo,
                VideoTitle = video.TituloVideo,
                CheckDate = DateTime.Now
            };

            try
            {
                // Usar el VideoService para verificar integridad con HMAC
                var integrityResponse = await _videoService.VerifyVideoIntegrityAsync(video.IdVideo, UserId);

                if (integrityResponse.Success && integrityResponse.Data != null)
                {
                    var integrityData = integrityResponse.Data;
                    result.IsValid = integrityData.IsValid;
                    result.FileExists = true;
                    result.HasMetadata = true;
                    result.FileSizeBytes = video.TamañoArchivo; // Usar el tamaño del video de la lista
                    result.FileHash = integrityData.HashSHA256Original;
                    result.ErrorMessage = integrityData.Message;
                }
                else
                {
                    // Error en la verificación
                    result.IsValid = false;
                    result.FileExists = false;
                    result.ErrorMessage = integrityResponse.Message ?? "Error al verificar integridad";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar integridad del video {VideoId}", video.IdVideo);
                result.IsValid = false;
                result.FileExists = false;
                result.ErrorMessage = $"Error durante la verificación: {ex.Message}";
            }

            return result;
        }
    }

    public class IntegrityCheckResult
    {
        public int VideoId { get; set; }
        public string VideoTitle { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool FileExists { get; set; }
        public bool HasMetadata { get; set; }
        public long FileSizeBytes { get; set; }
        public string? FileHash { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CheckDate { get; set; }
    }
}
