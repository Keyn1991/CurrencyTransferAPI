
namespace CurrencyTransferAPI.Services // lub CurrencyTransferAPI.DTOs
{
    public class TransactionListItemDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty; // <--- Inicjalizacja wartością domyślną
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty; // <--- Inicjalizacja wartością domyślną
        public string? Description { get; set; } // To już jest nullable, więc OK
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
    }
}