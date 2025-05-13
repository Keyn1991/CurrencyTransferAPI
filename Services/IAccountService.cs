using CurrencyTransferAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CurrencyTransferAPI.Services
{
    // DTO dla tworzenia konta (opcjonalne, ale może być przydatne)
    public class CreateAccountRequest
    {
        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters.")]
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; } = 0; // Domyślnie 0
    }

    // DTO dla odpowiedzi (możemy użyć bezpośrednio modelu Account lub stworzyć dedykowany)
    public class AccountResponse
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        // Możemy dodać UserId jeśli potrzebne na froncie
    }

    public interface IAccountService
    {
        Task<AccountResponse?> CreateAccountAsync(int userId, CreateAccountRequest request);
        Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId);
        Task<AccountResponse?> GetAccountByIdAsync(int accountId, int userId); // userId do weryfikacji właściciela
        // W przyszłości: Task<bool> UpdateBalanceAsync(int accountId, decimal amountChange);
    }
}