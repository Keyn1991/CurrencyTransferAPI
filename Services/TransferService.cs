// Plik: Services/TransferService.cs

using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransferService> _logger;

        public TransferService(ApplicationDbContext context, ILogger<TransferService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TransferResult> ExecuteTransferAsync(int userId, TransferRequestDto request)
        {
            // ... Twoja istniejąca logika do walidacji i wykonywania przelewu ...

            // POPRAWKA: Używamy wartości z enuma przy tworzeniu transakcji
            var transaction = new Transaction
            {
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                Amount = request.Amount,
                CurrencyCode = "XYZ", // Tutaj powinna być waluta konta
                Type = TransactionType.Transfer,
                Timestamp = DateTime.UtcNow,
                Description = request.Description
            };
            await _context.Transactions.AddAsync(transaction);
            // ...
            return new TransferResult { Success = true };
        }

        public async Task<IEnumerable<TransactionListItemDto>> GetTransactionsByUserIdAsync(int userId)
        {
            var userAccountIds = await _context.Accounts
                                            .Where(a => a.UserId == userId)
                                            .Select(a => a.Id)
                                            .ToListAsync();

            if (!userAccountIds.Any())
            {
                return Enumerable.Empty<TransactionListItemDto>();
            }

            return await _context.Transactions
                .Where(t => userAccountIds.Contains(t.FromAccountId ?? 0) || userAccountIds.Contains(t.ToAccountId))
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new TransactionListItemDto
                {
                    Id = t.Id,
                    Timestamp = t.Timestamp,
                    // POPRAWKA: Konwertujemy enum na string przy tworzeniu DTO
                    Type = t.Type.ToString(),
                    Amount = t.Amount,
                    CurrencyCode = t.CurrencyCode,
                    Description = t.Description,
                    FromAccountId = t.FromAccountId ?? 0,
                    ToAccountId = t.ToAccountId
                })
                .ToListAsync();
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null) return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
