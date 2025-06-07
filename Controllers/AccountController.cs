// Plik: Controllers/AccountController.cs
// Ten plik obsługuje wszystkie żądania związane z kontami bankowymi użytkowników.

using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    // POPRAWKA: Zmieniamy trasę na "api/Accounts" (liczba mnoga), aby pasowała do żądań z frontendu.
    [Route("api/Accounts")]
    [Authorize] // Wszystkie metody w tym kontrolerze wymagają autoryzacji
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        // Ujednolicona, poprawna metoda do odczytu ID użytkownika z tokenu
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            throw new InvalidOperationException("User ID not found in token or is invalid.");
        }

        /// <summary>
        /// Pobiera wszystkie konta zalogowanego użytkownika.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            try
            {
                var userId = GetCurrentUserId();
                var accounts = await _accountService.GetAccountsByUserIdAsync(userId);
                return Ok(accounts);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "GetMyAccounts: Error processing user identity from token.");
                return StatusCode(500, new { message = "An error occurred while processing your identity." });
            }
        }

        /// <summary>
        /// Tworzy nowe konto bankowe dla zalogowanego użytkownika.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var newAccount = await _accountService.CreateAccountAsync(userId, request);
                if (newAccount == null)
                {
                    return BadRequest("Could not create account. User not found or invalid currency.");
                }
                // Zwraca status 201 Created z lokalizacją nowego zasobu i jego danymi
                return CreatedAtAction(nameof(GetAccount), new { accountId = newAccount.Id }, newAccount);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "CreateAccount: Error processing user identity from token.");
                return StatusCode(500, new { message = "An error occurred while processing your identity." });
            }
        }

        /// <summary>
        /// Pobiera szczegóły konkretnego konta należącego do zalogowanego użytkownika.
        /// </summary>
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetAccount(int accountId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var account = await _accountService.GetAccountByIdAsync(accountId, userId);
                if (account == null) return NotFound();
                return Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "GetAccount: Error processing user identity from token.");
                return StatusCode(500, new { message = "An error occurred while processing your identity." });
            }
        }
    }
}
