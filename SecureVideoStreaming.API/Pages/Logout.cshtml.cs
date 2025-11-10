using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureVideoStreaming.API.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public IActionResult OnPost()
        {
            // Limpiar la sesión
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }
    }
}
