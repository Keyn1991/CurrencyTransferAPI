using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CurrencyTransferAPI.Services;   // Dla IAuthService, RegisterRequest, LoginRequest
using Microsoft.AspNetCore.Authorization; // Dla atrybutu [Authorize]
using System.Security.Claims;         // Dla dostępu do Claims (informacji z tokenu)
using CurrencyTransferAPI.Models;     // Dla UserRoles (jeśli używasz stałej UserRoles.Admin)
using System.IdentityModel.Tokens.Jwt;

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(model);

            if (result == null)
            {
                return BadRequest(new { message = "Username already exists or registration failed." });
            }
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(model);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }
            return Ok(result);
        }

        // --- NOWE METODY ---

        [HttpGet("me")]
        [Authorize] // Ten endpoint wymaga autoryzacji (ważnego tokenu JWT)
        public IActionResult GetCurrentUser()
        {
            // Dostęp do informacji o użytkowniku z tokenu JWT
            // User to obiekt ClaimsPrincipal dostarczany przez ASP.NET Core
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.NameId); // ID użytkownika (z claima "nameid")
            var userName = User.FindFirstValue(JwtRegisteredClaimNames.Sub); // Nazwa użytkownika (z claima "sub")
                                                                          // Alternatywnie User.Identity?.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role); // Rola użytkownika (z claima "role")
            var userEmail = User.FindFirstValue(ClaimTypes.Email); // Email (z claima "email")


            if (string.IsNullOrEmpty(userId)) // Sprawdzenie, czy udało się odczytać ID (choć [Authorize] powinno to zapewnić)
            {
                // To nie powinno się zdarzyć, jeśli token jest poprawny i [Authorize] działa
                return Unauthorized(new { message = "Could not identify user from token." });
            }

            return Ok(new {
                Id = userId,
                Username = userName,
                Email = userEmail,
                Role = userRole
            });
        }

        [HttpGet("adminarea")]
        [Authorize(Roles = UserRoles.Admin)] // Ten endpoint wymaga bycia zalogowanym ORAZ posiadania roli "Admin"
                                             // UserRoles.Admin pochodzi z CurrencyTransferAPI.Models.UserRoles
        public IActionResult GetAdminAreaData()
        {
            // Ten kod zostanie wykonany tylko jeśli użytkownik ma rolę "Admin"
            return Ok(new { message = "Welcome to the Admin Area! Only true admins can see this sacred place." });
        }
    }
}