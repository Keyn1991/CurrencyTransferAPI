// Plik: Services/IAccountService.cs

using CurrencyTransferAPI.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    // --- Data Transfer Objects (DTOs) dla Kont ---

    public class CreateAccountRequest
    {
        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Kod waluty musi składać się z 3 wielkich liter.")]
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public class AccountResponse
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public class AccountOperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public AccountResponse? AccountDetails { get; set; }
    }

    // --- Definicja Interfejsu ---

    public interface IAccountService
    {
        Task<AccountResponse?> CreateAccountAsync(int userId, CreateAccountRequest request);
        Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId);
        Task<AccountResponse?> GetAccountByIdAsync(int accountId, int userId);
        Task<AccountOperationResult> DepositAsync(int accountId, int userId, decimal amount, string description);

        // Metody dla Administratora
        Task<bool> DeleteAccountAsync(int accountId);
        Task<Account?> UpdateAccountBalanceAsync(int accountId, decimal newBalance);
    }
}
