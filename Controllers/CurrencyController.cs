using CurrencyTransferAPI.Models; // Zakładam, że CurrencyRequest i CurrencyResponse są w Models
using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System; // Dla Math.Round
using System.Threading.Tasks; // Dla Task

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Zmieniono na bardziej standardowe [controller] dla routingu
    public class CurrencyController : ControllerBase
    {
        private readonly NbpService _nbpService;
        private readonly ILogger<CurrencyController> _logger; // Dobrze jest dodać loggera

        public CurrencyController(NbpService nbpService, ILogger<CurrencyController> logger) // Dodaj loggera
        {
            _nbpService = nbpService;
            _logger = logger;
        }

        [HttpPost("convert")]
        public async Task<ActionResult<CurrencyResponse>> ConvertCurrency([FromBody] CurrencyRequest request)
        {
            if (string.IsNullOrEmpty(request.FromCurrency) || string.IsNullOrEmpty(request.ToCurrency) || request.Amount <= 0)
            {
                return BadRequest("Invalid request parameters: FromCurrency, ToCurrency, and Amount are required, and Amount must be positive.");
            }

            _logger.LogInformation("ConvertCurrency called: Amount {Amount} from {FromCurrency} to {ToCurrency}",
                request.Amount, request.FromCurrency, request.ToCurrency);

            // Pobieramy kursy obu walut względem PLN
            var fromRateInfo = await _nbpService.GetRateToPlnAsync(request.FromCurrency);
            var toRateInfo = await _nbpService.GetRateToPlnAsync(request.ToCurrency);

            if (fromRateInfo == null)
            {
                _logger.LogWarning("Could not get rate for FromCurrency: {FromCurrency}", request.FromCurrency);
                return BadRequest($"Unknown currency or problem fetching rate for the source currency: {request.FromCurrency}.");
            }
            if (toRateInfo == null)
            {
                _logger.LogWarning("Could not get rate for ToCurrency: {ToCurrency}", request.ToCurrency);
                return BadRequest($"Unknown currency or problem fetching rate for the destination currency: {request.ToCurrency}.");
            }

            // Kursy są teraz w fromRateInfo.Rate i toRateInfo.Rate
            // fromRateInfo.Rate to "ile PLN za 1 jednostkę FromCurrency"
            // toRateInfo.Rate to "ile PLN za 1 jednostkę ToCurrency"

            // Krok 1: Przelicz kwotę źródłową na PLN
            var amountInPln = request.Amount * fromRateInfo.Rate;

            // Krok 2: Przelicz kwotę w PLN na walutę docelową
            // Jeśli toRateInfo.Rate to 0 (co nie powinno się zdarzyć, jeśli GetRateToPlnAsync działa poprawnie dla PLN),
            // to dzielenie przez zero. Powinniśmy to obsłużyć.
            if (toRateInfo.Rate == 0)
            {
                 _logger.LogError("Rate for ToCurrency {ToCurrency} is zero, cannot perform division.", request.ToCurrency);
                return StatusCode(500, "Internal error: rate for destination currency is zero.");
            }
            var convertedAmount = amountInPln / toRateInfo.Rate;

            _logger.LogInformation("Conversion successful: {OriginalAmount} {FromCurrency} = {ConvertedAmount} {ToCurrency} (Rate FromPLN: {FromPlnRate}, Rate ToPLN: {ToPlnRate})",
                request.Amount, request.FromCurrency, convertedAmount, request.ToCurrency, fromRateInfo.Rate, toRateInfo.Rate);

            return Ok(new CurrencyResponse
            {
                ConvertedAmount = Math.Round(convertedAmount, 2), // Zazwyczaj zaokrąglamy do 2 miejsc dla walut pieniężnych
                Currency = request.ToCurrency,
                // Możesz dodać więcej informacji do odpowiedzi, np. użyty kurs
                // ExchangeRateUsed = Math.Round(fromRateInfo.Rate / toRateInfo.Rate, 4) // Kurs FromCurrency/ToCurrency
            });
        }
    }
}