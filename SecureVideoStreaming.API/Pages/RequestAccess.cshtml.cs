using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages;

public class RequestAccessModel : PageModel
{
    private readonly IVideoService _videoService;
    private readonly IPermissionService _permissionService;

    public RequestAccessModel(
        IVideoService videoService,
        IPermissionService permissionService)
    {
        _videoService = videoService;
        _permissionService = permissionService;
    }

    [BindProperty(SupportsGet = true)]
    public int VideoId { get; set; }

    public VideoResponse? Video { get; set; }

    [BindProperty]
    public string Justification { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Console.WriteLine($"[RequestAccess.OnGetAsync] VideoId: {VideoId}");
        
        // Verificar autenticación
        var userId = HttpContext.Session.GetInt32("UserId");
        Console.WriteLine($"[RequestAccess.OnGetAsync] UserId from Session: {userId}");
        
        if (!userId.HasValue)
        {
            Console.WriteLine("[RequestAccess.OnGetAsync] No user ID in session, redirecting to Login");
            return RedirectToPage("/Login");
        }

        var userType = HttpContext.Session.GetString("UserType");
        Console.WriteLine($"[RequestAccess.OnGetAsync] UserType: '{userType}'");
        
        // Permitir tanto "Usuario" como "Consumidor" (por si hay inconsistencias en la BD)
        if (userType == "Administrador")
        {
            ErrorMessage = "Los administradores no pueden solicitar acceso a videos";
            Console.WriteLine($"[RequestAccess.OnGetAsync] User is Administrador, redirecting to Home");
            return RedirectToPage("/Home");
        }

        // Obtener información del video
        Console.WriteLine($"[RequestAccess.OnGetAsync] Fetching video details for VideoId: {VideoId}");
        var videoResponse = await _videoService.GetVideoByIdAsync(VideoId);
        
        if (!videoResponse.Success || videoResponse.Data == null)
        {
            ErrorMessage = "Video no encontrado";
            Console.WriteLine($"[RequestAccess.OnGetAsync] Video not found, redirecting to VideoGrid");
            return RedirectToPage("/VideoGrid");
        }

        Video = videoResponse.Data;
        Console.WriteLine($"[RequestAccess.OnGetAsync] Video loaded successfully: {Video.TituloVideo}");
        Console.WriteLine("[RequestAccess.OnGetAsync] Returning Razor Page");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Verificar autenticación
        var userIdNullable = HttpContext.Session.GetInt32("UserId");
        if (!userIdNullable.HasValue)
        {
            return RedirectToPage("/Login");
        }
        
        int userId = userIdNullable.Value;

        var userType = HttpContext.Session.GetString("UserType");
        // Permitir tanto "Usuario" como "Consumidor" (por si hay inconsistencias en la BD)
        if (userType == "Administrador")
        {
            ErrorMessage = "Los administradores no pueden solicitar acceso a videos";
            return RedirectToPage("/Home");
        }

        // Validar justificación
        if (string.IsNullOrWhiteSpace(Justification))
        {
            ErrorMessage = "Debe proporcionar una justificación para la solicitud";
            
            // Recargar video
            var videoResponse = await _videoService.GetVideoByIdAsync(VideoId);
            if (videoResponse.Success && videoResponse.Data != null)
            {
                Video = videoResponse.Data;
            }
            
            return Page();
        }

        // Enviar solicitud usando PermissionService
        var result = await _permissionService.RequestAccessAsync(VideoId, userId, Justification);

        if (result.Success)
        {
            SuccessMessage = "Solicitud de acceso enviada correctamente. El administrador la revisará pronto.";
            return RedirectToPage("/VideoGrid");
        }
        else
        {
            ErrorMessage = result.Message ?? "Error al enviar la solicitud de acceso";
            
            // Recargar video
            var videoResponse = await _videoService.GetVideoByIdAsync(VideoId);
            if (videoResponse.Success && videoResponse.Data != null)
            {
                Video = videoResponse.Data;
            }
            
            return Page();
        }
    }
}
