using CurrencyTransferAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CurrencyTransferAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UtilsController : ControllerBase
    {
        [HttpGet("allowed-currencies")]
        public IActionResult GetAllowedCurrencies()
        {
            return Ok(AllowedCurrencies.GetAll());
        }
    }
}