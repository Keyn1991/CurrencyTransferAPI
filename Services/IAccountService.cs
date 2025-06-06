// Services/IAccountService.cs
using CurrencyTransferAPI.Models; // Dla Account
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // Potrzebne dla CreateAccountRequest

namespace CurrencyTransferAPI.Services
{
    // DTO dla tworzenia konta (już masz)
    public class CreateAccountRequest
    {
        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters.")]
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; } = 0;
    }

    // DTO dla odpowiedzi (już masz)
    public class AccountResponse
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    // --- NOWE DTO DLA WYNIKU OPERACJI NA KONCIE ---
    public class AccountOperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public AccountResponse? AccountDetails { get; set; } // Zwraca zaktualizowane dane konta
    }
    // --- KONIEC NOWEGO DTO ---


    public interface IAccountService
    {
        Task<AccountResponse?> CreateAccountAsync(int userId, CreateAccountRequest request);
        Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId);
        Task<AccountResponse?> GetAccountByIdAsync(int accountId, int userId);

        // --- NOWA METODA DLA DEPOZYTU ---
        /// <summary>
        /// Deposits funds into a specified account.
        /// </summary>
        /// <param name="accountId">The ID of the account to deposit into.</param>
        /// <param name="userId">The ID of the user performing the deposit (for ownership check).</param>
        /// <param name="amount">The amount to deposit.</param>
        /// <param name="description">Description of the deposit transaction.</param>
        /// <returns>An AccountOperationResult indicating the outcome.</returns>
        Task<AccountOperationResult> DepositAsync(int accountId, int userId, decimal amount, string description);
        // --- KONIEC NOWEJ METODY ---
    }
}