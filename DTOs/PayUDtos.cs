// DTOs/PayUDtos.cs
using System.ComponentModel.DataAnnotations; // Dla atrybutów walidacji

namespace CurrencyTransferAPI.DTOs // <--- WAŻNA PRZESTRZEŃ NAZW
{
    public class CreatePaymentRequestDto
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? CustomerIp { get; set; }
        public string? ContinueUrl { get; set; }
    }

    public class CreatePaymentResponseDto // <--- DEFINICJA TEGO TYPU
    {
        public string? OrderId { get; set; }
        public string? RedirectUri { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentStatusDto
    {
        public int AccountId { get; set; } // Upewnij się, że to pole tu jest
        public string? OrderId { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? CurrencyCode { get; set; }
    }
}