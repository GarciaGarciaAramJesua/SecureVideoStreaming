using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Services.Business.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SecureVideoStreaming.API.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        [BindProperty]
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Debe confirmar la contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Debe seleccionar un tipo de usuario")]
        public string UserType { get; set; } = "Usuario";

        [BindProperty]
        public string? PublicKeyRSA { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public RegisterModel(IAuthService authService)
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
                // Validar que si es Usuario (consumidor), debe tener clave pública
                if (UserType == "Usuario" && string.IsNullOrWhiteSpace(PublicKeyRSA))
                {
                    ErrorMessage = "Error al generar claves RSA. Por favor, intenta nuevamente.";
                    return Page();
                }

                var request = new RegisterUserRequest
                {
                    NombreUsuario = Username,
                    Email = Email,
                    Password = Password,
                    TipoUsuario = UserType,
                    ClavePublicaRSA = PublicKeyRSA // Incluir clave pública si es consumidor
                };

                var response = await _authService.RegisterAsync(request);

                if (!response.Success)
                {
                    ErrorMessage = response.Message ?? "Error al registrar usuario";
                    return Page();
                }

                SuccessMessage = "Usuario registrado exitosamente. Redirigiendo al login...";
                
                // Esperar 2 segundos y redirigir al login
                await Task.Delay(2000);
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar: {ex.Message}";
                return Page();
            }
        }
    }
}
