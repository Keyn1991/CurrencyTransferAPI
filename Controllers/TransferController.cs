// Plik: Controllers/TransferController.cs

using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Wszystkie metody w tym kontrolerze wymagają autoryzacji
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;
        private readonly ILogger<TransfersController> _logger;

        public TransfersController(ITransferService transferService, ILogger<TransfersController> logger)
        {
            _transferService = transferService;
            _logger = logger;
        }

        // --- Ujednolicona, poprawna metoda do odczytu ID użytkownika z tokenu ---
        private int GetCurrentUserId()
        {
            // Używamy standardowego ClaimTypes.NameIdentifier, aby znaleźć ID w tokenie.
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                // Jeśli nie uda się znaleźć lub sparsować ID, rzucamy błąd.
                // To jest bezpieczne, ponieważ atrybut [Authorize] już sprawdził, że token jest ważny.
                throw new InvalidOperationException("User ID not found in token or is invalid.");
            }
            return userId;
        }

        /// <summary>
        /// Tworzy nowy transfer pieniężny.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("CreateTransfer attempt by UserId {UserId} for FromAccountId {FromAccountId}", userId, request.FromAccountId);

                var result = await _transferService.ExecuteTransferAsync(userId, request);

                if (result.Success)
                {
                    _logger.LogInformation("Transfer successful for UserId {UserId}, TransactionId: {TransactionId}", userId, result.TransferDetails?.TransactionId);
                    return Ok(result.TransferDetails);
                }
                else
                {
                    _logger.LogWarning("Transfer failed for UserId {UserId}. Error: {ErrorMessage}", userId, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage ?? "Transfer processing failed." });
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "CreateTransfer: Error processing user identity from token.");
                return StatusCode(500, new { message = "An error occurred while processing your identity." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTransfer: An unexpected error occurred.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Pobiera historię transakcji dla zalogowanego użytkownika.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionListItemDto>>> GetUserTransactions()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Fetching transactions for UserId {UserId}", userId);

                var transactions = await _transferService.GetTransactionsByUserIdAsync(userId);
                return Ok(transactions);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "GetUserTransactions: Error processing user identity.");
                return StatusCode(500, new { message = "An error occurred while processing your identity." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserTransactions: An unexpected error occurred.");
                return StatusCode(500, new { message = "An unexpected error occurred while fetching transactions." });
            }
        }
    }
}
