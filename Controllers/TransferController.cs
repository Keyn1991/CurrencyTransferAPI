using CurrencyTransferAPI.Services; // Dla ITransferService i TransferRequestDto
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt; // Dla JwtRegisteredClaimNames

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;
        private readonly ILogger<TransfersController> _logger;

        public TransfersController(ITransferService transferService, ILogger<TransfersController> logger)
        {
            _transferService = transferService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            // Używamy JwtRegisteredClaimNames.NameId, jak ustaliliśmy
            var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.NameId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                _logger.LogError("GetCurrentUserId failed: Could not find or parse NameId claim. Value found: '{UserIdString}'", userIdString);
                throw new InvalidOperationException("User ID not found in token or is invalid.");
            }
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequestDto request)
        {
            _logger.LogInformation(
                "CreateTransfer endpoint called. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Amount: {Amount}",
                request.FromAccountId, request.ToAccountId, request.Amount);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateTransfer: ModelState is invalid.");
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId(); // ID użytkownika inicjującego przelew
                var result = await _transferService.ExecuteTransferAsync(userId, request);

                if (result.Success && result.TransferDetails != null)
                {
                    _logger.LogInformation("Transfer successful for UserId {UserId}. TransactionId: {TransactionId}", userId, result.TransferDetails.TransactionId);
                    return Ok(result.TransferDetails);
                }
                else
                {
                    _logger.LogWarning("Transfer failed for UserId {UserId}. Error: {ErrorMessage}", userId, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage ?? "Transfer processing failed." });
                }
            }
            catch (InvalidOperationException ex) // Specjalnie dla błędów z GetCurrentUserId
            {
                _logger.LogError(ex, "CreateTransfer: Error processing user identity from token.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error processing your identity: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTransfer: An unexpected error occurred.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again." });
            }
        }
    }
}