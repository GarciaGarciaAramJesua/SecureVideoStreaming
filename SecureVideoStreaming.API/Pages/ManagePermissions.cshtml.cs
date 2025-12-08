using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class ManagePermissionsModel : PageModel
    {
        private readonly IVideoService _videoService;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly ILogger<ManagePermissionsModel> _logger;

        public List<VideoListResponse> AdminVideos { get; set; } = new();
        public List<UserResponse> ConsumerUsers { get; set; } = new();
        public List<PermissionResponse> VideoPermissions { get; set; } = new();
        public List<PermissionResponse> PendingRequests { get; set; } = new();
        
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty]
        public int SelectedVideoId { get; set; }

        [BindProperty]
        public int SelectedUserId { get; set; }

        [BindProperty]
        public DateTime? FechaExpiracion { get; set; }

        [BindProperty]
        public int? LimiteAccesos { get; set; }

        public ManagePermissionsModel(
            IVideoService videoService,
            IPermissionService permissionService,
            IUserService userService,
            ILogger<ManagePermissionsModel> logger)
        {
            _videoService = videoService;
            _permissionService = permissionService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int? videoId = null)
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

                // Cargar usuarios consumidores
                var usersResponse = await _userService.GetAllUsersAsync();
                if (usersResponse != null)
                {
                    ConsumerUsers = usersResponse.Where(u => u.TipoUsuario == "Usuario" && u.Activo).ToList();
                }

                // Cargar solicitudes pendientes para los videos del admin
                await LoadPendingRequestsAsync(UserId);

                // Si se especificó un video, cargar sus permisos
                if (videoId.HasValue)
                {
                    SelectedVideoId = videoId.Value;
                    await LoadVideoPermissionsAsync(videoId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar datos de gestión de permisos");
                ErrorMessage = "Error al cargar los datos";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGrantPermissionAsync()
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
                return RedirectToPage("/ManagePermissions");
            }

            try
            {
                var request = new GrantPermissionRequest
                {
                    IdVideo = SelectedVideoId,
                    IdUsuario = SelectedUserId,
                    OtorgadoPor = userId.Value,
                    TipoPermiso = FechaExpiracion.HasValue ? "Temporal" : "Lectura",
                    FechaExpiracion = FechaExpiracion
                };

                var response = await _permissionService.GrantPermissionAsync(request);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Permiso otorgado correctamente";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo otorgar el permiso";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al otorgar permiso para video {VideoId} a usuario {UserId}", 
                    SelectedVideoId, SelectedUserId);
                TempData["ErrorMessage"] = "Error al otorgar el permiso";
            }

            return RedirectToPage("/ManagePermissions", new { videoId = SelectedVideoId });
        }

        public async Task<IActionResult> OnPostRevokePermissionAsync(int permissionId)
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
                return RedirectToPage("/ManagePermissions");
            }

            try
            {
                var response = await _permissionService.RevokePermissionAsync(permissionId, userId.Value);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Permiso revocado correctamente";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo revocar el permiso";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar permiso {PermissionId}", permissionId);
                TempData["ErrorMessage"] = "Error al revocar el permiso";
            }

            return RedirectToPage("/ManagePermissions", new { videoId = SelectedVideoId });
        }

        public async Task<IActionResult> OnPostExtendPermissionAsync(int permissionId, int additionalDays)
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
                return RedirectToPage("/ManagePermissions");
            }

            try
            {
                var newExpiration = DateTime.Now.AddDays(additionalDays);

                var response = await _permissionService.ExtendPermissionAsync(permissionId, newExpiration, userId.Value);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = $"Permiso extendido por {additionalDays} días";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo extender el permiso";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extender permiso {PermissionId}", permissionId);
                TempData["ErrorMessage"] = "Error al extender el permiso";
            }

            return RedirectToPage("/ManagePermissions", new { videoId = SelectedVideoId });
        }

        private async Task LoadVideoPermissionsAsync(int videoId)
        {
            try
            {
                var permissionsResponse = await _permissionService.GetPermissionsByVideoAsync(videoId, UserId);
                if (permissionsResponse.Success && permissionsResponse.Data != null)
                {
                    VideoPermissions = permissionsResponse.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar permisos del video {VideoId}", videoId);
            }
        }

        private async Task LoadPendingRequestsAsync(int adminId)
        {
            try
            {
                _logger.LogInformation("Cargando solicitudes pendientes para admin ID: {AdminId}", adminId);
                var pendingResponse = await _permissionService.GetPendingRequestsByAdminAsync(adminId);
                
                _logger.LogInformation("Respuesta del servicio - Success: {Success}, Data count: {Count}", 
                    pendingResponse.Success, 
                    pendingResponse.Data?.Count ?? 0);
                
                if (pendingResponse.Success && pendingResponse.Data != null)
                {
                    PendingRequests = pendingResponse.Data;
                    _logger.LogInformation("Se cargaron {Count} solicitudes pendientes", PendingRequests.Count);
                }
                else
                {
                    _logger.LogWarning("No se pudieron cargar solicitudes pendientes: {Message}", pendingResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar solicitudes pendientes para admin {AdminId}", adminId);
            }
        }

        public async Task<IActionResult> OnPostApproveRequestAsync(int permissionId, DateTime? fechaExpiracion, int? limiteAccesos)
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
                return RedirectToPage("/ManagePermissions");
            }

            try
            {
                var response = await _permissionService.ApprovePermissionAsync(permissionId, userId.Value, fechaExpiracion, limiteAccesos);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Solicitud aprobada correctamente";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo aprobar la solicitud";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar solicitud {PermissionId}", permissionId);
                TempData["ErrorMessage"] = "Error al aprobar la solicitud";
            }

            return RedirectToPage("/ManagePermissions");
        }

        public async Task<IActionResult> OnPostRejectRequestAsync(int permissionId)
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
                return RedirectToPage("/ManagePermissions");
            }

            try
            {
                var response = await _permissionService.RevokePermissionAsync(permissionId, userId.Value);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Solicitud rechazada";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo rechazar la solicitud";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar solicitud {PermissionId}", permissionId);
                TempData["ErrorMessage"] = "Error al rechazar la solicitud";
            }

            return RedirectToPage("/ManagePermissions");
        }
    }
}
