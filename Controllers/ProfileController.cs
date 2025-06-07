// Plik: Controllers/ProfileController.cs

using CurrencyTransferAPI.Services; // POPRAWKA: Dodano brakujący 'using', aby znaleźć definicje
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;


namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IUserService userService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _userService.GetUserProfileByIdAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var success = await _userService.UpdateUserProfileAsync(userId, dto);
            if (!success) return NotFound();
            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            var userId = GetCurrentUserId();
            var success = await _userService.DeleteUserAsync(userId);
            if (!success) return NotFound();
            return Ok(new { message = "Account deleted successfully." });
        }
    }
}
