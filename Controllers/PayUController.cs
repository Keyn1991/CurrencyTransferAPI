// Controllers/PayUController.cs
using CurrencyTransferAPI.DTOs;
using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims; // Dla ClaimsPrincipal
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt; // Dla JwtRegisteredClaimNames

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Cały kontroler wymaga autoryzacji
    public class PayUController : ControllerBase
    {
        private readonly IPayUService _payUService;
        private readonly ILogger<PayUController> _logger;
        private readonly IAccountService _accountService; // Potrzebny do pobrania konta użytkownika

        public PayUController(IPayUService payUService, ILogger<PayUController> logger, IAccountService accountService)
        {
            _payUService = payUService;
            _logger = logger;
            _accountService = accountService;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.NameId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                throw new InvalidOperationException("User ID not found or invalid in token.");
            }
            return userId;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Dodatkowa weryfikacja: czy konto AccountId należy do zalogowanego użytkownika?
            var userId = GetCurrentUserId();
            var account = await _accountService.GetAccountByIdAsync(request.AccountId, userId);
            if (account == null)
            {
                _logger.LogWarning("User {UserId} attempted to create payment for account {AccountId} they do not own or does not exist.", userId, request.AccountId);
                return Forbid("You can only create payments for your own accounts.");
            }
            // Upewnij się, że waluta w requeście zgadza się z walutą konta (PayUService też to sprawdza, ale można i tu)
             if (account.CurrencyCode != request.CurrencyCode) {
                return BadRequest(new { message = $"Payment currency ({request.CurrencyCode}) must match the currency of the selected account ({account.CurrencyCode})."});
            }


            // Przekazanie adresu IP klienta, jeśli jest dostępny i potrzebny
            request.CustomerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Ustawienie ContinueUrl na frontendowy URL, który obsłuży wynik "płatności"
            // Frontend powinien mieć stronę /payment-status lub /payment-return
            request.ContinueUrl = "http://localhost:3000/payment-status"; // Zmień na swój URL frontendu

            var result = await _payUService.CreatePaymentAsync(request);

            if (result.Success)
            {
                // W prawdziwym scenariuszu, frontend użyłby result.RedirectUri
                // aby przekierować użytkownika na stronę PayU.
                // Tutaj zwracamy go, aby frontend mógł go "symulować".
                return Ok(result);
            }
            else
            {
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to create payment." });
            }
        }

        [HttpGet("payment-status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return BadRequest("Order ID is required.");
            }

            var statusResult = await _payUService.GetPaymentStatusAsync(orderId);

            if (statusResult.Status == "NOT_FOUND")
            {
                return NotFound(statusResult);
            }

            // W tym miejscu, jeśli statusResult.Status == "COMPLETED",
            // PayUService już powinien był spróbować zaktualizować saldo konta.
            // Możemy tu dodatkowo sprawdzić, czy konto faktycznie należy do użytkownika,
            // ale logika "doładowania" jest w PayUService.
            return Ok(statusResult);
        }
    }
}