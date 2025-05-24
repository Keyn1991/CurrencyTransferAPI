using System; // Dla DateTime
using System.ComponentModel.DataAnnotations; // Dla atrybutów walidacji ([Required], [Range], [MaxLength])
using System.Threading.Tasks; // Dla Task

namespace CurrencyTransferAPI.Services
{
    // --- Data Transfer Objects (DTOs) for Transfers ---

    public class TransferRequestDto
    {
        [Required(ErrorMessage = "Source account ID is required.")]
        public int FromAccountId { get; set; }

        [Required(ErrorMessage = "Destination account ID is required.")]
        public int ToAccountId { get; set; }

        [Required(ErrorMessage = "Transfer amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.00.")]
        public decimal Amount { get; set; }

        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; } // Opcjonalny opis/tytuł przelewu
    }

    public class TransferResponseDto
    {
        public int TransactionId { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal AmountTransferred { get; set; } // Kwota faktycznie przelana (w walucie transakcji)
        public string CurrencyCode { get; set; } = string.Empty; // Waluta transakcji
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }
        public decimal NewSourceAccountBalance { get; set; } // Saldo konta źródłowego po przelewie
        // Można dodać NewDestinationAccountBalance, jeśli potrzebne
    }

    // Klasa pomocnicza do zwracania wyniku operacji transferu,
    // zawierająca status powodzenia, komunikat błędu (jeśli wystąpił)
    // oraz szczegóły udanego transferu.
    public class TransferResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TransferResponseDto? TransferDetails { get; set; } // Null, jeśli Success == false
    }


    // --- Interface Definition for Transfer Service ---

    public interface ITransferService
    {
        /// <summary>
        /// Executes a currency transfer between two accounts.
        /// </summary>
        /// <param name="initiatingUserId">The ID of the user initiating the transfer (for permission checks).</param>
        /// <param name="request">The details of the transfer request.</param>
        /// <returns>A TransferResult indicating the outcome of the operation.</returns>
        Task<TransferResult> ExecuteTransferAsync(int initiatingUserId, TransferRequestDto request);

                // --- НОВЫЙ МЕТОД ДЛЯ ПОЛУЧЕНИЯ ТРАНЗАКЦИЙ ---
                Task<IEnumerable<TransactionListItemDto>> GetTransactionsByUserIdAsync(int userId);
                // --- КОНЕЦ НОВОГО МЕТОДА ---
    }
}