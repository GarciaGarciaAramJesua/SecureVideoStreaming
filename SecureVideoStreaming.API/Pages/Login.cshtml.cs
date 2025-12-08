using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.API.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;

        [BindProperty]
        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        public void OnGet()
        {
            // Si ya está logueado, redirigir al home
            if (HttpContext.Session.GetString("Username") != null)
            {
                Response.Redirect("/Home");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor, completa todos los campos correctamente.";
                return Page();
            }

            try
            {
                var request = new LoginRequest
                {
                    Email = Email,
                    Password = Password
                };

                var response = await _authService.LoginAsync(request);

                if (!response.Success)
                {
                    ErrorMessage = response.Message ?? "Credenciales inválidas";
                    return Page();
                }

                // Guardar información en sesión
                HttpContext.Session.SetString("Token", response.Token ?? "");
                HttpContext.Session.SetString("Username", response.Username ?? "");
                HttpContext.Session.SetString("Email", response.Email ?? "");
                HttpContext.Session.SetString("UserType", response.UserType ?? "");
                HttpContext.Session.SetInt32("UserId", response.UserId);

                // Redirigir a página intermedia que verifica configuración de claves
                return RedirectToPage("/LoginRedirect");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al iniciar sesión: {ex.Message}";
                return Page();
            }
        }
    }
}
