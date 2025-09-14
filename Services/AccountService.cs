// Plik: Services/AccountService.cs
// Ten plik zawiera TYLKO implementację logiki serwisu.

using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    // Klasa implementuje teraz oba interfejsy: IAccountService i IUserService
    public class AccountService : IAccountService, IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountService> _logger;

        public AccountService(ApplicationDbContext context, ILogger<AccountService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- Metody z IAccountService ---

        public async Task<AccountResponse?> CreateAccountAsync(int userId, CreateAccountRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var account = new Account
            {
                UserId = userId,
                CurrencyCode = request.CurrencyCode.ToUpper(),
                Balance = 0
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return new AccountResponse { Id = account.Id, CurrencyCode = account.CurrencyCode, Balance = account.Balance };
        }

        public async Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountResponse { Id = a.Id, CurrencyCode = a.CurrencyCode, Balance = a.Balance })
                .ToListAsync();
        }

        public async Task<AccountResponse?> GetAccountByIdAsync(int accountId, int userId)
        {
            var account = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
            if (account == null) return null;
            return new AccountResponse { Id = account.Id, CurrencyCode = account.CurrencyCode, Balance = account.Balance };
        }

        public async Task<AccountOperationResult> DepositAsync(int accountId, int userId, decimal amount, string description)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
            if (account == null)
            {
                return new AccountOperationResult { Success = false, ErrorMessage = "Konto nie zostało znalezione lub nie masz do niego dostępu." };
            }

            account.Balance += amount;

            var transaction = new Transaction { ToAccountId = account.Id, Amount = amount, CurrencyCode = account.CurrencyCode, Type = TransactionType.Deposit, Timestamp = System.DateTime.UtcNow, Description = description };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return new AccountOperationResult
            {
                Success = true,
                AccountDetails = new AccountResponse { Id = account.Id, Balance = account.Balance, CurrencyCode = account.CurrencyCode }
            };
        }

        public async Task<bool> DeleteAccountAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return false;
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Account?> UpdateAccountBalanceAsync(int accountId, decimal newBalance)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return null;
            account.Balance = newBalance;
            await _context.SaveChangesAsync();
            return account;
        }


        // --- Metody z IUserService ---

        public async Task<UserProfileDto?> GetUserProfileByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateUserProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                user.Email = dto.Email;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
