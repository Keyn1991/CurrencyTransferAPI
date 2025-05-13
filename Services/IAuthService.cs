using CurrencyTransferAPI.Models;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CurrencyTransferAPI.Services
{

    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest model);
        Task<AuthResponse?> LoginAsync(LoginRequest model);
    }
}