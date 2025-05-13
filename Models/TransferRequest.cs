namespace CurrencyTransferAPI.Models
{
    public class TransferRequest
    {
        public string Receiver { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}