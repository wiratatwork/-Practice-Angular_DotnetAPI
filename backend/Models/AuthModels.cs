using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public UserDto User { get; set; } = new();
    }

    public class LoginResponse : TokenResponse
    {
    }

    public class RefreshResponse : TokenResponse
    {
    }
}
