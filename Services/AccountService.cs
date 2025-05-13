using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            _logger.LogInformation("CreateAccountAsync: User {UserId}, Currency {Currency}", userId, request.CurrencyCode);

            if (!await _context.Users.AnyAsync(u => u.Id == userId))
            {
                _logger.LogWarning("CreateAccountAsync: User {UserId} not found.", userId);
                return null;
            }

            if (!AllowedCurrencies.IsAllowed(request.CurrencyCode))
            {
                _logger.LogWarning("CreateAccountAsync: Invalid currency {Currency} for User {UserId}.", request.CurrencyCode, userId);
                return null; // Sygnalizacja dla kontrolera
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

        // ... Zachowaj istniejące metody GetAccountsByUserIdAsync i GetAccountByIdAsync ...
        public async Task<IEnumerable<AccountResponse>> GetAccountsByUserIdAsync(int userId)
        {
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
    }
}