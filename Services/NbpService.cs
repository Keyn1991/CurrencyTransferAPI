using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace CurrencyTransferAPI.Services
{
    public class NbpService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public NbpService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<decimal?> GetRateAsync(string currency)
        {
            if (currency.ToUpper() == "PLN")
                return 1m;

            if (_cache.TryGetValue(currency.ToUpper(), out decimal cachedRate))
            {
                return cachedRate;
            }

            try
            {
                var response = await _httpClient.GetFromJsonAsync<NbpApiResponse>(
                    $"https://api.nbp.pl/api/exchangerates/rates/A/{currency}?format=json");

                var rate = response?.Rates.FirstOrDefault()?.Mid;

                if (rate.HasValue)
                {
                    _cache.Set(currency.ToUpper(), rate.Value, TimeSpan.FromMinutes(30));
                }

                return rate;
            }
            catch
            {
                return null;
            }
        }

        private class NbpApiResponse
        {
            public List<NbpRate> Rates { get; set; } = new();
        }

        private class NbpRate
        {
            public decimal Mid { get; set; }
        }
    }
}
