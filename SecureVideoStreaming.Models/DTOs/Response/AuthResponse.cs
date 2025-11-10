namespace SecureVideoStreaming.Models.DTOs.Response
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
