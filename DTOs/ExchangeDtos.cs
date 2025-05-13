using System;
using System.ComponentModel.DataAnnotations;

namespace CurrencyTransferAPI.DTOs
{
    public class ExchangeRequestDto
    {
        [Required]
        public int FromAccountId { get; set; }

        [Required]
        public int ToAccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount to exchange must be greater than 0.00.")]
        public decimal AmountToExchange { get; set; }
    }

    public class ExchangeResponseDto
    {
        public int TransactionId { get; set; }
        public int FromAccountId { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public decimal AmountDebited { get; set; }
        public int ToAccountId { get; set; }
        public string ToCurrency { get; set; } = string.Empty;
        public decimal AmountCredited { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal NewFromAccountBalance { get; set; }
        public decimal NewToAccountBalance { get; set; }
    }

    public class ExchangeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public ExchangeResponseDto? ExchangeDetails { get; set; }
    }
}