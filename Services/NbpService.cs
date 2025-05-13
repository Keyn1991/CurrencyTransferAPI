using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json; // Dla JsonException
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class RateInfo
    {
        public decimal Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class NbpService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NbpService> _logger;

        public NbpService(HttpClient httpClient, IMemoryCache cache, ILogger<NbpService> logger)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://api.nbp.pl/api/");
            }
            _cache = cache;
            _logger = logger;
        }

        public async Task<RateInfo?> GetRateToPlnAsync(string currencyCode)
        {
            currencyCode = currencyCode.ToUpper();
            if (currencyCode == "PLN")
                return new RateInfo { Rate = 1m, EffectiveDate = DateTime.UtcNow };

            string cacheKey = $"NBP_Rate_{currencyCode}_PLN";
            if (_cache.TryGetValue(cacheKey, out RateInfo? cachedRateInfo))
            {
                if (cachedRateInfo != null)
                {
                    _logger.LogInformation("Cache hit for {CurrencyCode}/PLN rate: {Rate} as of {Date}", currencyCode, cachedRateInfo.Rate, cachedRateInfo.EffectiveDate);
                    return cachedRateInfo;
                }
            }

            _logger.LogInformation("Fetching rate for {CurrencyCode}/PLN from NBP API.", currencyCode);
            try
            {
                NbpApiResponse? response = await _httpClient.GetFromJsonAsync<NbpApiResponse>($"exchangerates/rates/A/{currencyCode}?format=json");
                var rateData = response?.Rates?.FirstOrDefault();

                if (rateData != null && rateData.Mid > 0 && !string.IsNullOrEmpty(rateData.EffectiveDate) && DateTime.TryParse(rateData.EffectiveDate, out var effectiveDate))
                {
                    var rateInfo = new RateInfo { Rate = rateData.Mid, EffectiveDate = effectiveDate };
                    _cache.Set(cacheKey, rateInfo, TimeSpan.FromHours(1));
                    _logger.LogInformation("Fetched and cached rate for {CurrencyCode}/PLN: {Rate} as of {Date}", currencyCode, rateInfo.Rate, rateInfo.EffectiveDate);
                    return rateInfo;
                }

                _logger.LogWarning("Could not parse rate or effective date for {CurrencyCode}/PLN from NBP response. Response Code: {Code}, Currency: {Currency}, RateData Mid: {MidValue}, RateData EffectiveDate: {DateValue}",
                    currencyCode, response?.Code, response?.Currency, rateData?.Mid, rateData?.EffectiveDate);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "NBP API HttpRequestException for {CurrencyCode}/PLN. Status: {StatusCode}, Message: {ErrorMessage}", currencyCode, ex.StatusCode, ex.Message);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "NBP API JsonException for {CurrencyCode}/PLN. Error parsing JSON. Message: {ErrorMessage}", currencyCode, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching NBP rate for {CurrencyCode}/PLN.", currencyCode);
                return null;
            }
        }

        public async Task<RateInfo?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            fromCurrency = fromCurrency.ToUpper();
            toCurrency = toCurrency.ToUpper();

            if (fromCurrency == toCurrency)
                return new RateInfo { Rate = 1m, EffectiveDate = DateTime.UtcNow };

            _logger.LogInformation("Attempting to get exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);

            RateInfo? rateFromToPln = null;
            RateInfo? rateToToPln = null;

            if (fromCurrency == "PLN")
            {
                rateToToPln = await GetRateToPlnAsync(toCurrency); // toCurrency to np. USD
                if (rateToToPln != null && rateToToPln.Rate > 0)
                {
                    // Log: "Kurs USD/PLN: X.XX. Obliczanie kursu PLN/USD."
                    _logger.LogInformation("Fetched rate {ToCurrency}/PLN: {Rate}. Calculating PLN/{ToCurrencyTarget} rate.",
                                           toCurrency, rateToToPln.Rate, toCurrency); // POPRAWIONY LOG
                    return new RateInfo { Rate = 1 / rateToToPln.Rate, EffectiveDate = rateToToPln.EffectiveDate };
                }
            }
            else if (toCurrency == "PLN")
            {
                rateFromToPln = await GetRateToPlnAsync(fromCurrency);
                if (rateFromToPln != null)
                {
                     _logger.LogInformation("Rate {FromCurrency}/PLN: {Rate}.", fromCurrency, rateFromToPln.Rate);
                    return rateFromToPln;
                }
            }
            else
            {
                _logger.LogInformation("Calculating cross rate for {FromCurrency}/{ToCurrency} via PLN.", fromCurrency, toCurrency);
                rateFromToPln = await GetRateToPlnAsync(fromCurrency);
                rateToToPln = await GetRateToPlnAsync(toCurrency);

                if (rateFromToPln != null && rateFromToPln.Rate > 0 && rateToToPln != null && rateToToPln.Rate > 0)
                {
                    decimal crossRate = rateFromToPln.Rate / rateToToPln.Rate;
                    _logger.LogInformation("Cross rate {FromCurrency}/{ToCurrency}: ({FromPlnRate} / {ToPlnRate}) = {CalculatedCrossRate}",
                        fromCurrency, toCurrency, rateFromToPln.Rate, rateToToPln.Rate, crossRate);
                    return new RateInfo
                    {
                        Rate = crossRate,
                        EffectiveDate = rateFromToPln.EffectiveDate < rateToToPln.EffectiveDate ? rateFromToPln.EffectiveDate : rateToToPln.EffectiveDate
                    };
                }
            }

            _logger.LogWarning("Could not determine exchange rate between {FromCurrency} and {ToCurrency}. RateFromToPln: {@RateFromDetails}, RateToToPln: {@RateToDetails}",
                fromCurrency, toCurrency, rateFromToPln, rateToToPln);
            return null;
        }

        private class NbpApiResponse
        {
            public string? Table { get; set; }
            public string? Currency { get; set; }
            public string? Code { get; set; }
            public List<NbpRate>? Rates { get; set; }
        }

        private class NbpRate
        {
            public string? No { get; set; }
            public string? EffectiveDate { get; set; }
            public decimal Mid { get; set; }
        }
    }
}