using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.API.Pages
{
    public class SetupKeysModel : PageModel
    {
        private readonly IUserService _userService;

        public SetupKeysModel(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult OnGet()
        {
            // Verificar que el usuario esté logueado
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            // Verificar que sea un usuario consumidor
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Usuario")
            {
                // Los administradores no necesitan claves RSA del lado del cliente
                return RedirectToPage("/Home");
            }

            return Page();
        }

        /// <summary>
        /// Endpoint para registrar la clave pública del usuario
        /// </summary>
        public async Task<IActionResult> OnPostRegisterPublicKeyAsync([FromBody] RegisterPublicKeyRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Usuario no autenticado" });
                }

                // Registrar la clave pública en la base de datos
                var response = await _userService.UpdatePublicKeyAsync(userId.Value, request.PublicKey, request.Fingerprint);

                if (!response.Success)
                {
                    return new JsonResult(new { success = false, message = response.Message });
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    public class RegisterPublicKeyRequest
    {
        public string PublicKey { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
    }
}
