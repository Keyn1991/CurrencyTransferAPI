using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using CurrencyTransferAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class ExchangeService : IExchangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly NbpService _nbpService;
        private readonly ILogger<ExchangeService> _logger;

        public ExchangeService(ApplicationDbContext context, NbpService nbpService, ILogger<ExchangeService> logger)
        {
            _context = context;
            _nbpService = nbpService;
            _logger = logger;
        }

        public async Task<ExchangeResult> PerformExchangeAsync(int userId, ExchangeRequestDto request)
        {
            _logger.LogInformation("Exchange attempt: UserId {UserId}, FromAccId {FromAccId}, ToAccId {ToAccId}, Amount {Amount}",
                userId, request.FromAccountId, request.ToAccountId, request.AmountToExchange);

            if (request.FromAccountId == request.ToAccountId)
                return new ExchangeResult { Success = false, ErrorMessage = "Source and destination accounts for exchange cannot be the same physical account." };
            if (request.AmountToExchange <= 0)
                return new ExchangeResult { Success = false, ErrorMessage = "Amount to exchange must be positive." };

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var fromAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId);
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.ToAccountId && a.UserId == userId);

                if (fromAccount == null || toAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    return new ExchangeResult { Success = false, ErrorMessage = "One or both accounts not found or do not belong to the user." };
                }

                if (fromAccount.CurrencyCode == toAccount.CurrencyCode)
                {
                    await dbTransaction.RollbackAsync();
                    return new ExchangeResult { Success = false, ErrorMessage = "Accounts must have different currencies for exchange." };
                }

                if (fromAccount.Balance < request.AmountToExchange)
                {
                    await dbTransaction.RollbackAsync();
                    return new ExchangeResult { Success = false, ErrorMessage = "Insufficient funds in the source account." };
                }

                var rateInfo = await _nbpService.GetExchangeRateAsync(fromAccount.CurrencyCode, toAccount.CurrencyCode);
                if (rateInfo == null || rateInfo.Rate <= 0)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError("Could not retrieve or invalid exchange rate for {FromCcy} to {ToCcy}", fromAccount.CurrencyCode, toAccount.CurrencyCode);
                    return new ExchangeResult { Success = false, ErrorMessage = $"Could not retrieve a valid exchange rate for {fromAccount.CurrencyCode}/{toAccount.CurrencyCode}." };
                }
                decimal exchangeRate = rateInfo.Rate;
                decimal amountCredited = Math.Round(request.AmountToExchange * exchangeRate, 4);

                if(amountCredited <= 0)
                {
                    await dbTransaction.RollbackAsync();
                    return new ExchangeResult { Success = false, ErrorMessage = "Calculated exchange amount is zero or negative after applying the exchange rate." };
                }

                fromAccount.Balance -= request.AmountToExchange;
                toAccount.Balance += amountCredited;

                _context.Accounts.UpdateRange(fromAccount, toAccount);

                var transaction = new Transaction
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountId = toAccount.Id,
                    Amount = request.AmountToExchange,
                    CurrencyCode = fromAccount.CurrencyCode,
                    Type = TransactionType.Exchange,
                    Timestamp = DateTime.UtcNow,
                    Description = $"Exchange from {fromAccount.CurrencyCode} to {toAccount.CurrencyCode}. Rate: {exchangeRate:F4}."
                };
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Exchange successful: TxId {TxId}, Debited {DebAmount} {DebCcy}, Credited {CreAmount} {CreCcy}",
                    transaction.Id, request.AmountToExchange, fromAccount.CurrencyCode, amountCredited, toAccount.CurrencyCode);

                return new ExchangeResult
                {
                    Success = true,
                    ExchangeDetails = new ExchangeResponseDto
                    {
                        TransactionId = transaction.Id,
                        FromAccountId = fromAccount.Id,
                        FromCurrency = fromAccount.CurrencyCode,
                        AmountDebited = request.AmountToExchange,
                        ToAccountId = toAccount.Id,
                        ToCurrency = toAccount.CurrencyCode,
                        AmountCredited = amountCredited,
                        ExchangeRate = exchangeRate,
                        Timestamp = transaction.Timestamp,
                        NewFromAccountBalance = fromAccount.Balance,
                        NewToAccountBalance = toAccount.Balance
                    }
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error during currency exchange for UserId {UserId}", userId);
                return new ExchangeResult { Success = false, ErrorMessage = "An unexpected error occurred during the exchange." };
            }
        }
    }
}