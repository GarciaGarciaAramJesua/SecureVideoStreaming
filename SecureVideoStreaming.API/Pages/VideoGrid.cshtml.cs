using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class VideoGridModel : PageModel
    {
        private readonly IVideoGridService _videoGridService;
        private readonly ILogger<VideoGridModel> _logger;

        public VideoGridModel(
            IVideoGridService videoGridService,
            ILogger<VideoGridModel> logger)
        {
            _videoGridService = videoGridService;
            _logger = logger;
        }

        // Propiedades para la vista
        public List<VideoGridItemResponse> Videos { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? PermissionStatus { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? AdminName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Verificar autenticación mediante sesión
                var userIdSession = HttpContext.Session.GetInt32("UserId");
                if (!userIdSession.HasValue)
                {
                    _logger.LogWarning("Usuario no autenticado. Redirigiendo a Login");
                    TempData["ErrorMessage"] = "Debes iniciar sesión para ver el grid de videos";
                    return RedirectToPage("/Login");
                }

                // Verificar que es Owner (Administrador)
                var userType = HttpContext.Session.GetString("UserType");
                if (userType != "Owner")
                {
                    _logger.LogWarning("Usuario Consumer intentó acceder al grid. Redirigiendo a Home");
                    TempData["ErrorMessage"] = "Solo los administradores pueden acceder a la galería de videos";
                    return RedirectToPage("/Home");
                }

                int userId = userIdSession.Value;

                // Si hay filtros, usar búsqueda filtrada
                if (!string.IsNullOrWhiteSpace(SearchTerm) || 
                    !string.IsNullOrWhiteSpace(PermissionStatus) || 
                    !string.IsNullOrWhiteSpace(AdminName))
                {
                    bool? soloConPermiso = PermissionStatus == "Activo" ? true : null;
                    
                    var response = await _videoGridService.GetVideoGridWithFiltersAsync(
                        userId,
                        SearchTerm,
                        AdminName,
                        soloConPermiso
                    );
                    
                    if (response.Success && response.Data != null)
                    {
                        Videos = response.Data;
                        
                        // Filtrar por estado de permiso si se especificó
                        if (!string.IsNullOrWhiteSpace(PermissionStatus))
                        {
                            Videos = Videos.Where(v => v.EstadoPermiso == PermissionStatus).ToList();
                        }
                    }
                    
                    _logger.LogInformation(
                        "Búsqueda filtrada para usuario {UserId}: {Count} videos encontrados. Filtros: Term='{SearchTerm}', Admin='{AdminName}', Status='{PermissionStatus}'",
                        userId, Videos.Count, SearchTerm, AdminName, PermissionStatus
                    );
                }
                else
                {
                    // Obtener todos los videos sin filtros
                    var response = await _videoGridService.GetVideoGridForUserAsync(userId);
                    
                    if (response.Success && response.Data != null)
                    {
                        Videos = response.Data;
                    }
                    
                    _logger.LogInformation(
                        "Usuario {UserId} cargó el grid completo: {Count} videos totales",
                        userId, Videos.Count
                    );
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el grid de videos para el usuario");
                TempData["ErrorMessage"] = "Error al cargar la galería de videos. Por favor, intenta nuevamente.";
                Videos = new List<VideoGridItemResponse>();
                return Page();
            }
        }
    }
}
