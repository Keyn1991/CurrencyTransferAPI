// Plik: Services/IAuthService.cs

using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    /// <summary>
    /// Kontrakt (interfejs) dla serwisu autoryzacji.
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest model);
        Task<AuthResponse?> LoginAsync(LoginRequest model);
        
        // POPRAWKA: Dodajemy brakującą definicję metody do interfejsu.
        Task SeedAdminUserAsync();
    }

    /// <summary>
    /// Obiekt dla żądania rejestracji.
    /// </summary>
    public class RegisterRequest 
    { 
        public string Username { get; set; } = ""; 
        public string Email { get; set; } = ""; 
        public string Password { get; set; } = ""; 
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    /// <summary>
    /// Obiekt dla żądania logowania.
    /// </summary>
    public class LoginRequest 
    { 
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } = ""; 
    }

    /// <summary>
    /// Obiekt dla odpowiedzi po udanej autoryzacji.
    /// </summary>
    public class AuthResponse 
    { 
        public string Token { get; set; } = ""; 
        public string Username { get; set; } = ""; 
        public IList<string> Roles { get; set; } = new List<string>(); 
    }
}
