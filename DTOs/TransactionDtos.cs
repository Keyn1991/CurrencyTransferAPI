
namespace CurrencyTransferAPI.Services
{
    public class TransactionListItemDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
    }
}