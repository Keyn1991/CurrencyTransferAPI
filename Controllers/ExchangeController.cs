using CurrencyTransferAPI.DTOs;
using CurrencyTransferAPI.Services; // Dla ExchangeResult
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt; // Dla JwtRegisteredClaimNames
using System.Security.Claims;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeService _exchangeService;
        private readonly ILogger<ExchangeController> _logger;

        public ExchangeController(IExchangeService exchangeService, ILogger<ExchangeController> logger)
        {
            _exchangeService = exchangeService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.NameId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                // Ten wyjątek zostanie złapany przez globalny handler lub try-catch w akcji
                throw new InvalidOperationException("User ID not found in token or is invalid.");
            }
            return userId;
        }

        [HttpPost("perform")]
        public async Task<IActionResult> PerformExchange([FromBody] ExchangeRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                // Tworzymy obiekt pasujący do struktury ExchangeResult dla spójności odpowiedzi błędów
                var errorResult = new ExchangeResult
                {
                    Success = false,
                    ErrorMessage = "Invalid request data. Please check the provided information."
                    // Można by tu dodać szczegóły z ModelState, jeśli jest taka potrzeba
                };
                return BadRequest(errorResult);
            }

            try
            {
                var userId = GetCurrentUserId();
                ExchangeResult result = await _exchangeService.PerformExchangeAsync(userId, request);

                if (result.Success) // ExchangeService ustawia Success i ExchangeDetails
                {
                    _logger.LogInformation("Exchange operation successful for UserId {UserId}, FromAcc {FromAcc}, ToAcc {ToAcc}",
                        userId, request.FromAccountId, request.ToAccountId);
                    return Ok(result); // Zwróć cały obiekt ExchangeResult (który zawiera ExchangeDetails)
                }
                else // ExchangeService ustawił Success = false i ErrorMessage
                {
                    _logger.LogWarning("Exchange operation failed for UserId {UserId}. Reason: {Error}",
                        userId, result.ErrorMessage);
                    return BadRequest(result); // Zwróć cały obiekt ExchangeResult z błędem
                }
            }
            catch (InvalidOperationException ex) // Z GetCurrentUserId
            {
                _logger.LogError(ex, "PerformExchange: Error processing user identity.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ExchangeResult { Success = false, ErrorMessage = "An error occurred while processing your identity: " + ex.Message });
            }
            catch (Exception ex) // Inne nieoczekiwane błędy
            {
                _logger.LogError(ex, "PerformExchange: An unexpected error occurred for UserId while processing exchange request.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ExchangeResult { Success = false, ErrorMessage = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}