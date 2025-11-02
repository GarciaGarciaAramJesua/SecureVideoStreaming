using SecureVideoStreaming.Models.Enums;

namespace SecureVideoStreaming.Models.DTOs.Request
{
    public class RegisterUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public string PublicKeyRsa { get; set; } = string.Empty; // Generada en el cliente
    }
}