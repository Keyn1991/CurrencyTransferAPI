// Services/AccountService.cs
using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models; // Dla Account, Transaction, TransactionType
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System; // Dla DateTime
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountService> _logger;

        public AccountService(ApplicationDbContext context, ILogger<AccountService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AccountResponse?> CreateAccountAsync(int userId, CreateAccountRequest request)
        {
            // ... (Twój istniejący kod CreateAccountAsync - wygląda dobrze)
            _logger.LogInformation("CreateAccountAsync: User {UserId}, Currency {Currency}", userId, request.CurrencyCode);

            if (!await _context.Users.AnyAsync(u => u.Id == userId))
            {
                _logger.LogWarning("CreateAccountAsync: User {UserId} not found.", userId);
                return null;
            }

            if (!AllowedCurrencies.IsAllowed(request.CurrencyCode))
            {
                _logger.LogWarning("CreateAccountAsync: Invalid currency {Currency} for User {UserId}.", request.CurrencyCode, userId);
                return null;
            }

            var currencyCodeUpper = request.CurrencyCode.ToUpper();
            if (await _context.Accounts.AnyAsync(a => a.UserId == userId && a.CurrencyCode == currencyCodeUpper))
            {
                _logger.LogWarning("CreateAccountAsync: Account in {Currency} already exists for User {UserId}.", currencyCodeUpper, userId);
                return null;
            }

            var account = new Account
            {
                UserId = userId,
                CurrencyCode = currencyCodeUpper,
                Balance = request.InitialBalance
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            _logger.LogInformation("CreateAccountAsync: Account {AccountId} created for User {UserId} in {Currency}.", account.Id, userId, currencyCodeUpper);

            return new AccountResponse { Id = account.Id, CurrencyCode = account.CurrencyCode, Balance = account.Balance };
        }

        public async Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId)
        {
            // ... (Twój istniejący kod GetAccountsByUserIdAsync - wygląda dobrze)
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountResponse
                {
                    Id = a.Id,
                    CurrencyCode = a.CurrencyCode,
                    Balance = a.Balance
                })
                .ToListAsync();
        }

        public async Task<AccountResponse?> GetAccountByIdAsync(int accountId, int userId)
        {
            // ... (Twój istniejący kod GetAccountByIdAsync - wygląda dobrze)
            var account = await _context.Accounts
                .Where(a => a.Id == accountId && a.UserId == userId)
                .Select(a => new AccountResponse
                {
                    Id = a.Id,
                    CurrencyCode = a.CurrencyCode,
                    Balance = a.Balance
                })
                .SingleOrDefaultAsync();
            return account;
        }

        // --- IMPLEMENTACJA NOWEJ METODY DEPOSITASYNC ---
        public async Task<AccountOperationResult> DepositAsync(int accountId, int userId, decimal amount, string description)
        {
            _logger.LogInformation("Attempting deposit to AccountId {AccountId} by UserId {UserId} for Amount {Amount}", accountId, userId, amount);

            if (amount <= 0)
            {
                _logger.LogWarning("Deposit failed: Amount must be positive. Amount: {Amount}", amount);
                return new AccountOperationResult { Success = false, ErrorMessage = "Deposit amount must be positive." };
            }

            // Rozpoczęcie transakcji bazodanowej dla spójności
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var account = await _context.Accounts
                                        .FirstOrDefaultAsync(a => a.Id == accountId); // Pobieramy konto

                if (account == null)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Deposit failed: Account {AccountId} not found.", accountId);
                    return new AccountOperationResult { Success = false, ErrorMessage = "Account not found." };
                }

                // Opcjonalna weryfikacja, czy użytkownik ma prawo do tego konta,
                // jeśli userId ma oznaczać właściciela konta.
                // Dla systemowego depozytu (np. z PayU) ta weryfikacja może być inna lub pominięta,
                // jeśli system płatności ma odpowiednie uprawnienia.
                // Na razie zakładamy, że weryfikacja właściciela nie jest tu kluczowa,
                // bo AccountId powinno być już zweryfikowane wcześniej (np. w kontrolerze PayU).
                // if (account.UserId != userId)
                // {
                //     await dbTransaction.RollbackAsync();
                //     _logger.LogWarning("Deposit failed: User {UserId} does not own Account {AccountId}.", userId, accountId);
                //     return new AccountOperationResult { Success = false, ErrorMessage = "User does not own this account." };
                // }

                // Aktualizacja salda
                account.Balance += amount;
                _context.Accounts.Update(account);

                // Utworzenie rekordu transakcji dla depozytu
                var depositTransaction = new Transaction
                {
                    // Dla depozytu FromAccountId może być specjalnym kontem systemowym
                    // lub tym samym kontem, na które wpływają środki, dla uproszczenia.
                    // W modelu Transaction FromAccountId i ToAccountId są Required.
                    FromAccountId = accountId, // Można rozważyć dedykowane konto systemowe dla źródła depozytów
                    ToAccountId = accountId,
                    Amount = amount,
                    CurrencyCode = account.CurrencyCode, // Waluta depozytu to waluta konta
                    Type = TransactionType.Deposit,    // Upewnij się, że TransactionType.Deposit istnieje w enumie
                    Timestamp = DateTime.UtcNow,
                    Description = description
                };
                await _context.Transactions.AddAsync(depositTransaction);

                await _context.SaveChangesAsync(); // Zapisz zmiany salda i nową transakcję
                await dbTransaction.CommitAsync(); // Zatwierdź transakcję bazodanową

                _logger.LogInformation("Deposit successful to AccountId {AccountId}. New balance: {NewBalance}", accountId, account.Balance);

                return new AccountOperationResult
                {
                    Success = true,
                    AccountDetails = new AccountResponse // Zwracamy zaktualizowane dane konta
                    {
                        Id = account.Id,
                        CurrencyCode = account.CurrencyCode,
                        Balance = account.Balance
                    }
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error during deposit to AccountId {AccountId}", accountId);
                return new AccountOperationResult { Success = false, ErrorMessage = "An unexpected error occurred during the deposit." };
            }
        }
        // --- KONIEC IMPLEMENTACJI NOWEJ METODY ---
    }
}