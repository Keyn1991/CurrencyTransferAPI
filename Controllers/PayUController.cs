using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PayUController : ControllerBase
    {
        private readonly IPayUService _payUService;
        private readonly ILogger<PayUController> _logger;

        public PayUController(IPayUService payUService, ILogger<PayUController> logger)
        {
            _payUService = payUService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            throw new InvalidOperationException("User ID not found or invalid in token.");
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var result = await _payUService.CreatePaymentAsync(userId, request);

                if (result.Success)
                    return Ok(result);

                return BadRequest(new { message = result.ErrorMessage ?? "Failed to create PayU payment." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreatePayment.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}
