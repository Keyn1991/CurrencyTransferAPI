namespace CurrencyTransferAPI.Models
{
    public class CurrencyResponse
    {
        public decimal ConvertedAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
