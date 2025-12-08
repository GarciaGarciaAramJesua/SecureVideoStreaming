using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureVideoStreaming.API.Pages
{
    public class LoginRedirectModel : PageModel
    {
        public string UserType { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Verificar que el usuario esté logueado
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            UserType = HttpContext.Session.GetString("UserType") ?? "";
            Token = HttpContext.Session.GetString("Token") ?? "";
            
            return Page();
        }
    }
}
