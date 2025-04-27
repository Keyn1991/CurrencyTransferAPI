using Microsoft.AspNetCore.Mvc;
using CurrencyTransferAPI.Models;

namespace CurrencyTransferAPI.Controllers
{
    [ApiController]
    [Route("api/transfer")]
    public class TransferController : ControllerBase
    {
        private static readonly List<string> Transactions = new();

        [HttpPost("send")]
        public ActionResult<TransferResponse> SendMoney([FromBody] TransferRequest request)
        {
            Transactions.Add($"Sent {request.Amount} {request.Currency} to {request.Receiver}");
            return Ok(new TransferResponse { Status = "Success", Message = "Money sent." });
        }

        [HttpPost("receive")]
        public ActionResult<TransferResponse> ReceiveMoney([FromBody] TransferRequest request)
        {
            Transactions.Add($"Received {request.Amount} {request.Currency} from {request.Receiver}");
            return Ok(new TransferResponse { Status = "Success", Message = "Money received." });
        }

        [HttpGet("transactions")]
        public ActionResult<List<string>> GetTransactions()
        {
            return Ok(Transactions);
        }
    }
}
