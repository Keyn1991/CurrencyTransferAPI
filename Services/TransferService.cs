// Services/TransferService.cs
using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // <--- ДОБАВЛЕНО для IEnumerable
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransferService> _logger;
        // private readonly NbpService _nbpService; // Если понадобится для конвертации в будущем

        public TransferService(ApplicationDbContext context, ILogger<TransferService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TransferResult> ExecuteTransferAsync(int userId, TransferRequestDto request)
        {
            _logger.LogInformation(
                "Attempting transfer initiated by UserId {UserId}: FromAccountId {FromAccountId} ToAccountId {ToAccountId}, Amount: {Amount}, Description: {Description}",
                userId, request.FromAccountId, request.ToAccountId, request.Amount, request.Description);

            if (request.FromAccountId == request.ToAccountId)
            {
                _logger.LogWarning("Transfer failed: Source and destination accounts are the same (AccountId: {AccountId}).", request.FromAccountId);
                return new TransferResult { Success = false, ErrorMessage = "Source and destination accounts cannot be the same." };
            }

            if (request.Amount <= 0)
            {
                 _logger.LogWarning("Transfer failed: Invalid transfer amount ({Amount}). Amount must be positive.", request.Amount);
                return new TransferResult { Success = false, ErrorMessage = "Transfer amount must be positive." };
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId);

                if (fromAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Source account {FromAccountId} not found or does not belong to user {UserId}.", request.FromAccountId, userId);
                    return new TransferResult { Success = false, ErrorMessage = "Source account not found or you do not have permission to access it." };
                }

                if (fromAccount.Balance < request.Amount)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Insufficient funds in source account {FromAccountId}. Current balance: {Balance}, Requested: {Amount}",
                        fromAccount.Id, fromAccount.Balance, request.Amount);
                    return new TransferResult { Success = false, ErrorMessage = "Insufficient funds in the source account." };
                }

                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.ToAccountId);
                if (toAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Destination account {ToAccountId} not found.", request.ToAccountId);
                    return new TransferResult { Success = false, ErrorMessage = "Destination account not found." };
                }

                if (fromAccount.CurrencyCode != toAccount.CurrencyCode)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Cross-currency transfer from {FromCurrency} to {ToCurrency} is not yet supported.",
                        fromAccount.CurrencyCode, toAccount.CurrencyCode);
                    return new TransferResult { Success = false, ErrorMessage = "Cross-currency transfers are not supported at this time. Accounts must be in the same currency." };
                }
                string transactionCurrency = fromAccount.CurrencyCode;
                decimal amountToCredit = request.Amount;


                fromAccount.Balance -= request.Amount;
                toAccount.Balance += amountToCredit;

                _context.Accounts.Update(fromAccount);
                _context.Accounts.Update(toAccount);

                var transaction = new Transaction
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountId = toAccount.Id,
                    Amount = request.Amount,
                    CurrencyCode = transactionCurrency, // <--- Убедись, что это правильно!
                    Type = TransactionType.Transfer,
                    Timestamp = DateTime.UtcNow,
                    Description = request.Description
                };
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Transfer successful: TransactionId {TransactionId}, {Amount} {Currency} from AccountId {FromAccountId} (new balance: {NewFromBalance}) to AccountId {ToAccountId} (new balance: {NewToBalance}).",
                    transaction.Id, request.Amount, transactionCurrency, fromAccount.Id, fromAccount.Balance, toAccount.Id, toAccount.Balance);

                return new TransferResult
                {
                    Success = true,
                    TransferDetails = new TransferResponseDto
                    {
                        TransactionId = transaction.Id,
                        FromAccountId = fromAccount.Id,
                        ToAccountId = toAccount.Id,
                        AmountTransferred = request.Amount,
                        CurrencyCode = transactionCurrency,
                        Timestamp = transaction.Timestamp,
                        Description = transaction.Description,
                        NewSourceAccountBalance = fromAccount.Balance
                    }
                };
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Database update error during transfer from AccountId {FromAccountId} to AccountId {ToAccountId}.", request.FromAccountId, request.ToAccountId);
                return new TransferResult { Success = false, ErrorMessage = "A database error occurred while processing the transfer." };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "An unexpected error occurred during the transfer from AccountId {FromAccountId} to AccountId {ToAccountId}.", request.FromAccountId, request.ToAccountId);
                return new TransferResult { Success = false, ErrorMessage = "An unexpected error occurred during the transfer." };
            }
        }

        // --- РЕАЛИЗАЦИЯ НОВОГО МЕТОДА GetTransactionsByUserIdAsync ---
        public async Task<IEnumerable<TransactionListItemDto>> GetTransactionsByUserIdAsync(int userId)
        {
            _logger.LogInformation("Fetching transactions for UserId {UserId}", userId);
            try
            {
                var userAccountIds = await _context.Accounts
                                                .Where(a => a.UserId == userId)
                                                .Select(a => a.Id)
                                                .ToListAsync();

                if (!userAccountIds.Any())
                {
                    _logger.LogInformation("No accounts found for UserId {UserId}. Returning empty transaction list.", userId);
                    return Enumerable.Empty<TransactionListItemDto>();
                }

                var transactions = await _context.Transactions
                    .Where(t => userAccountIds.Contains(t.FromAccountId) || userAccountIds.Contains(t.ToAccountId))
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => new TransactionListItemDto
                    {
                        Id = t.Id,
                        Timestamp = t.Timestamp,
                        Type = t.Type.ToString(),
                        Amount = t.Amount,
                        CurrencyCode = t.CurrencyCode,
                        Description = t.Description,
                        FromAccountId = t.FromAccountId,
                        ToAccountId = t.ToAccountId
                        // Если решишь добавить AccountNumber в DTO, здесь нужно будет их загружать
                        // Например, через Include в запросе и маппинг:
                        // FromAccountNumber = t.FromAccount != null ? t.FromAccount.AccountNumber : null,
                        // ToAccountNumber = t.ToAccount != null ? t.ToAccount.AccountNumber : null,
                        // (Это если у Account есть свойство AccountNumber)
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} transactions for UserId {UserId}", transactions.Count, userId);
                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transactions for UserId {UserId}", userId);
                // В зависимости от твоей политики обработки ошибок, ты можешь:
                // 1. Пробросить исключение дальше (контроллер его поймает и вернет 500)
                // throw;
                // 2. Вернуть пустой список и залогировать ошибку (как сейчас)
                // 3. Вернуть специальный объект Result с информацией об ошибке
                return Enumerable.Empty<TransactionListItemDto>(); // Возвращаем пустой список в случае ошибки для простоты
            }
        }
        // --- КОНЕЦ РЕАЛИЗАЦИИ НОВОГО МЕТОДА ---
    }
}