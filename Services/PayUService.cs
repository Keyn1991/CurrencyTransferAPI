using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CurrencyTransferAPI.Services
{
    public class PayUService : IPayUService
    {
        private readonly ILogger<PayUService> _logger;

        public PayUService(ILogger<PayUService> logger)
        {
            _logger = logger;
        }

        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(int userId, CreatePaymentRequestDto request)
        {
            _logger.LogInformation("Creating PayU payment for user {UserId} and account {AccountId}", userId, request.AccountId);

            // Twoja logika integracji z PayU

            await Task.Delay(50); // symulacja

            return new CreatePaymentResponseDto
            {
                Success = true,
                OrderId = "XYZ123",
                RedirectUri = $"https://secure.payu.com/pay/?orderId=XYZ123&continueUrl={request.ContinueUrl}"
            };
        }

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(string orderId)
        {
            // Logika statusu płatności

            await Task.Delay(50);

            return new PaymentStatusDto
            {
                AccountId = 1,
                OrderId = orderId,
                Status = "COMPLETED",
                Amount = 100,
                CurrencyCode = "PLN"
            };
        }
    }
}
