using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureVideoStreaming.API.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // Si el usuario ya está logueado, redirigir al home
            if (HttpContext.Session.GetString("Username") != null)
            {
                Response.Redirect("/Home");
            }
        }
    }
}
