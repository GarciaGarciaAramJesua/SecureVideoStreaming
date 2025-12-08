using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class MyPermissionsModel : PageModel
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<MyPermissionsModel> _logger;

        public List<PermissionResponse> ActivePermissions { get; set; } = new();
        public List<PermissionResponse> ExpiredPermissions { get; set; } = new();
        public List<PermissionResponse> AllPermissions { get; set; } = new();
        
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Estadísticas
        public int TotalPermisos { get; set; }
        public int PermisosActivos { get; set; }
        public int PermisosExpirados { get; set; }
        public int PermisosProximosAExpirar { get; set; }

        public MyPermissionsModel(
            IPermissionService permissionService,
            ILogger<MyPermissionsModel> logger)
        {
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

            Username = username;
            var userType = HttpContext.Session.GetString("UserType");

            if (userType != "Usuario")
            {
                TempData["ErrorMessage"] = "Esta página es solo para usuarios consumidores";
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
                // Cargar todos los permisos del usuario
                var permissionsResponse = await _permissionService.GetPermissionsByUserAsync(UserId);
                if (permissionsResponse.Success && permissionsResponse.Data != null)
                {
                    AllPermissions = permissionsResponse.Data;

                    // Clasificar permisos
                    ActivePermissions = AllPermissions.Where(p => p.EstaActivo).ToList();
                    ExpiredPermissions = AllPermissions.Where(p => !p.EstaActivo).ToList();

                    // Calcular estadísticas
                    TotalPermisos = AllPermissions.Count;
                    PermisosActivos = ActivePermissions.Count;
                    PermisosExpirados = ExpiredPermissions.Count;

                    // Permisos que expiran en menos de 7 días
                    var now = DateTime.Now;
                    var sevenDaysFromNow = now.AddDays(7);
                    PermisosProximosAExpirar = ActivePermissions.Count(p => 
                        p.FechaExpiracion.HasValue && 
                        p.FechaExpiracion.Value <= sevenDaysFromNow &&
                        p.FechaExpiracion.Value > now);
                }
                else
                {
                    ErrorMessage = permissionsResponse.Message ?? "No se pudieron cargar los permisos";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar permisos del usuario {UserId}", UserId);
                ErrorMessage = "Error al cargar los permisos";
            }

            return Page();
        }
    }
}
