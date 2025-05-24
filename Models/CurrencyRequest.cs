namespace CurrencyTransferAPI.Models
{
    public class CurrencyRequest
    {
        public string FromCurrency { get; set; } = string.Empty;
           public string ToCurrency { get; set; } = string.Empty;
           public decimal Amount { get; set; }
    }
}
