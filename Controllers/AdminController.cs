// Plik: Controllers/AdminController.cs

using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // KRYTYCZNE: Ten atrybut zapewnia, że tylko użytkownicy z rolą "Admin" mają dostęp.
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ITransferService _transferService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAccountService accountService, ITransferService transferService, ILogger<AdminController> logger)
        {
            _accountService = accountService;
            _transferService = transferService;
            _logger = logger;
        }

        // --- Zarządzanie Kontami ---

        /// <summary>
        /// [Admin] Usuwa konto bankowe użytkownika.
        /// </summary>
        [HttpDelete("accounts/{accountId}")]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            _logger.LogInformation("Admin request to delete account ID: {AccountId}", accountId);
            var result = await _accountService.DeleteAccountAsync(accountId);
            if (!result)
            {
                return NotFound(new { message = "Account not found or could not be deleted." });
            }
            return Ok(new { message = "Account deleted successfully." });
        }

        /// <summary>
        /// [Admin] Aktualizuje saldo konta. (Operacja do używania z ostrożnością!)
        /// </summary>
        [HttpPut("accounts/{accountId}")]
        public async Task<IActionResult> UpdateAccount(int accountId, [FromBody] UpdateAccountRequest dto)
        {
            _logger.LogInformation("Admin request to update account ID: {AccountId}", accountId);
            var updatedAccount = await _accountService.UpdateAccountBalanceAsync(accountId, dto.NewBalance);
            if (updatedAccount == null)
            {
                return NotFound(new { message = "Account not found." });
            }
            return Ok(updatedAccount);
        }

        // --- Zarządzanie Transakcjami ---

        /// <summary>
        /// [Admin] Usuwa transakcję.
        /// </summary>
        [HttpDelete("transactions/{transactionId}")]
        public async Task<IActionResult> DeleteTransaction(int transactionId)
        {
            _logger.LogInformation("Admin request to delete transaction ID: {TransactionId}", transactionId);
            var result = await _transferService.DeleteTransactionAsync(transactionId);
            if (!result)
            {
                return NotFound(new { message = "Transaction not found or could not be deleted." });
            }
            return Ok(new { message = "Transaction deleted successfully." });
        }
    }

    // Prosty DTO do aktualizacji konta
    public class UpdateAccountRequest
    {
        public decimal NewBalance { get; set; }
    }
}
