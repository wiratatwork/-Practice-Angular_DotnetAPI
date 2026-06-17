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

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
