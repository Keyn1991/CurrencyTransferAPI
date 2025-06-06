using CurrencyTransferAPI.DTOs; // <--- DODAJ TĘ LINIĘ
using System.Collections.Generic; // Dla IEnumerable, jeśli używasz go dla TransactionListItemDto
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    // Jeśli TransactionListItemDto jest zdefiniowany w DTOs/TransactionDtos.cs,
    // to też będzie potrzebny `using CurrencyTransferAPI.DTOs;` (lub odpowiedni namespace)
    // jeśli TransactionListItemDto jest używany w tym interfejsie.

    // Tutaj też masz definicje DTO dla transferów, upewnij się, że są one
    // albo w tym samym namespace co interfejs, albo jest odpowiedni using.
    // W Twoim poprzednim kodzie były one w namespace CurrencyTransferAPI.Services, więc było OK.

    public interface IPayUService
    {
        Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request); // Teraz CreatePaymentResponseDto powinno być widoczne
        Task<PaymentStatusDto> GetPaymentStatusAsync(string orderId);
        // Task<bool> ProcessWebhookNotificationAsync(PayUWebhookNotification notification);
    }
}