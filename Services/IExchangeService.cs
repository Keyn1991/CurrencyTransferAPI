using CurrencyTransferAPI.DTOs;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public interface IExchangeService
    {
        Task<ExchangeResult> PerformExchangeAsync(int userId, ExchangeRequestDto request);
    }
}