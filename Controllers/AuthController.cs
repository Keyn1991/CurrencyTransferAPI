// Plik: Controllers/AuthController.cs

using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var result = await _authService.RegisterAsync(model);
            if (result == null)
            {
                return BadRequest(new { message = "Username or email already exists." });
            }
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            var result = await _authService.LoginAsync(model);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }
            return Ok(result);
        }

        // Ten endpoint jest teraz poprawnie chroniony i odczytuje dane z tokenu
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            if (User.Identity?.IsAuthenticated == false)
            {
                return Unauthorized(new { message = "Could not identify user from token." });
            }

            return Ok(new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Username = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
