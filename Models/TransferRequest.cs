namespace CurrencyTransferAPI.Models
{
    public class TransferRequest
    {
        public string Receiver { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
