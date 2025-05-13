using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using CurrencyTransferAPI.Models; // Dla AllowedCurrencies
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.NameId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                _logger.LogError("GetCurrentUserId failed: Could not find or parse NameId claim. Value found: '{UserIdString}'", userIdString);
                throw new InvalidOperationException("User ID not found in token or is invalid.");
            }
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            _logger.LogInformation("CreateAccount endpoint called with CurrencyCode: {CurrencyCode}", request.CurrencyCode);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateAccount: ModelState is invalid.");
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var accountResponse = await _accountService.CreateAccountAsync(userId, request);

                if (accountResponse == null)
                {
                    if (!AllowedCurrencies.IsAllowed(request.CurrencyCode))
                    {
                        _logger.LogWarning("CreateAccount failed: Invalid currency code {CurrencyCode} by UserId {UserId}.", request.CurrencyCode, userId);
                        return BadRequest(new { message = $"Invalid currency code '{request.CurrencyCode}'. Allowed currencies are: {string.Join(", ", AllowedCurrencies.GetAll())}." });
                    }
                    _logger.LogWarning("CreateAccount failed for UserId {UserId}, Currency {CurrencyCode}. Account may already exist or user invalid.", userId, request.CurrencyCode);
                    return BadRequest(new { message = "Could not create account. It may already exist for this currency or the user is invalid." });
                }
                return CreatedAtAction(nameof(GetAccountById), new { accountId = accountResponse.Id }, accountResponse);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "CreateAccount: Error processing user identity from token.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing your identity: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAccount: An unexpected error occurred.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again later."});
            }
        }

        // ... Zachowaj istniejące metody GetUserAccounts i GetAccountById ...
        [HttpGet]
        public async Task<IActionResult> GetUserAccounts()
        {
            _logger.LogInformation("GetUserAccounts endpoint called.");
            try
            {
                var userId = GetCurrentUserId();
                var accounts = await _accountService.GetAccountsByUserIdAsync(userId);
                return Ok(accounts);
            }
            catch (InvalidOperationException ex)
            {
                 _logger.LogError(ex, "GetUserAccounts: Error processing user identity from token.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing your identity: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserAccounts: An unexpected error occurred.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again later."});
            }
        }

        [HttpGet("{accountId:int}")]
        public async Task<IActionResult> GetAccountById(int accountId)
        {
            _logger.LogInformation("GetAccountById endpoint called for accountId: {AccountId}", accountId);
            try
            {
                var userId = GetCurrentUserId();
                var account = await _accountService.GetAccountByIdAsync(accountId, userId);

                if (account == null)
                {
                    _logger.LogWarning("GetAccountById: Account not found or no permission for accountId {AccountId} and UserId {UserId}", accountId, userId);
                    return NotFound(new { message = "Account not found or you do not have permission to access it." });
                }
                return Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "GetAccountById: Error processing user identity from token for accountId {AccountId}.", accountId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing your identity: " + ex.Message });
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "GetAccountById: An unexpected error occurred for accountId {AccountId}.", accountId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again later."});
            }
        }
    }
}