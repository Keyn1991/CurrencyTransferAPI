using CurrencyTransferAPI.Models;
using CurrencyTransferAPI.Services;
using Microsoft.AspNetCore.Mvc;


namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/currency")]
    public class CurrencyController : ControllerBase
    {
        private readonly NbpService _nbpService;

        public CurrencyController(NbpService nbpService)
        {
            _nbpService = nbpService;
        }

        [HttpPost("convert")]
        public async Task<ActionResult<CurrencyResponse>> ConvertCurrency([FromBody] CurrencyRequest request)
        {
            var fromRate = await _nbpService.GetRateAsync(request.FromCurrency);
            var toRate = await _nbpService.GetRateAsync(request.ToCurrency);

            if (fromRate == null || toRate == null)
            {
                return BadRequest("Unknown currency or problem fetching rates.");
            }

            var plnAmount = request.Amount * fromRate.Value;
            var convertedAmount = plnAmount / toRate.Value;

            return Ok(new CurrencyResponse
            {
                ConvertedAmount = Math.Round(convertedAmount, 2),
                Currency = request.ToCurrency
            });
        }
    }
}
