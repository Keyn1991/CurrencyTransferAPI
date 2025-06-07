// Plik: Services/IPayUService.cs
// Ten plik zawiera TYLKO definicje interfejsu i DTO dla PayU.

using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    // --- Data Transfer Objects (DTOs) dla PayU ---
    // Te definicje są teraz w JEDNYM, poprawnym miejscu.

    public class CreatePaymentRequestDto
    {
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContinueUrl { get; set; } = string.Empty;
    }

    public class CreatePaymentResponseDto
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public string? RedirectUri { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentStatusDto
    {
        public int AccountId { get; set; }
        public string? OrderId { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }

    // --- Definicja Interfejsu ---

    public interface IPayUService
    {
        Task<CreatePaymentResponseDto> CreatePaymentAsync(int userId, CreatePaymentRequestDto request);
        Task<PaymentStatusDto> GetPaymentStatusAsync(string orderId);
    }
}
