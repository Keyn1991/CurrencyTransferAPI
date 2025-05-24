namespace CurrencyTransferAPI.Services // или CurrencyTransferAPI.DTOs
{
    public class TransactionListItemDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // "Transfer", "Exchange", etc.
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string? Description { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        // Можно добавить номера счетов отправителя/получателя, если это легко сделать в сервисе
        // public string? FromAccountNumber { get; set; }
        // public string? ToAccountNumber { get; set; }
    }
}