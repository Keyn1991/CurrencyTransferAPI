using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq; // Potrzebne dla FirstOrDefaultAsync
using System.Threading.Tasks;
// using CurrencyTransferAPI.DTOs; // Jeśli DTO są w osobnym namespace

namespace CurrencyTransferAPI.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransferService> _logger;
        // private readonly NbpService _nbpService; // Odkomentuj, jeśli będziesz robić przeliczenia walut

        public TransferService(ApplicationDbContext context, ILogger<TransferService> logger /*, NbpService nbpService */)
        {
            _context = context;
            _logger = logger;
            // _nbpService = nbpService;
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

            if (request.Amount <= 0) // Dodatkowa walidacja, chociaż Range powinien to złapać
            {
                 _logger.LogWarning("Transfer failed: Invalid transfer amount ({Amount}). Amount must be positive.", request.Amount);
                return new TransferResult { Success = false, ErrorMessage = "Transfer amount must be positive." };
            }

            // Rozpoczęcie transakcji bazodanowej
            // To jest kluczowe dla spójności danych!
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Pobierz konto źródłowe, upewnij się, że należy do użytkownika i zablokuj wiersz na czas transakcji
                // Użycie .Set<Account>() z .FromSqlRaw lub .ExecuteSqlRaw dla SELECT ... FOR UPDATE byłoby bardziej zaawansowane
                // dla blokowania, ale EF Core domyślnie używa odpowiedniego poziomu izolacji dla transakcji.
                // Na razie proste pobranie powinno wystarczyć, ale dla systemów o wysokiej współbieżności warto to rozważyć.
                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId);

                if (fromAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Source account {FromAccountId} not found or does not belong to user {UserId}.", request.FromAccountId, userId);
                    return new TransferResult { Success = false, ErrorMessage = "Source account not found or you do not have permission to access it." };
                }

                // 2. Sprawdź saldo konta źródłowego
                if (fromAccount.Balance < request.Amount)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Insufficient funds in source account {FromAccountId}. Current balance: {Balance}, Requested: {Amount}",
                        fromAccount.Id, fromAccount.Balance, request.Amount);
                    return new TransferResult { Success = false, ErrorMessage = "Insufficient funds in the source account." };
                }

                // 3. Pobierz konto docelowe
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.ToAccountId);
                if (toAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Destination account {ToAccountId} not found.", request.ToAccountId);
                    return new TransferResult { Success = false, ErrorMessage = "Destination account not found." };
                }

                // === OBSŁUGA WALUT ===
                // Na razie zakładamy, że przelewy są możliwe tylko w tej samej walucie.
                // Walutą transakcji będzie waluta konta źródłowego.
                if (fromAccount.CurrencyCode != toAccount.CurrencyCode)
                {
                    // TODO: Implementacja logiki dla przelewów międzywalutowych
                    // - Pobierz kurs wymiany (np. z NbpService)
                    // - Przelicz `request.Amount` na walutę konta docelowego
                    // - Upewnij się, że `Transaction.CurrencyCode` i `Transaction.Amount` odzwierciedlają
                    //   walutę i kwotę pobraną z konta źródłowego.
                    await dbTransaction.RollbackAsync();
                    _logger.LogWarning("Transfer failed: Cross-currency transfer from {FromCurrency} to {ToCurrency} is not yet supported.",
                        fromAccount.CurrencyCode, toAccount.CurrencyCode);
                    return new TransferResult { Success = false, ErrorMessage = "Cross-currency transfers are not supported at this time. Accounts must be in the same currency." };
                }
                string transactionCurrency = fromAccount.CurrencyCode;
                decimal amountToCredit = request.Amount; // W tym scenariuszu kwota do zaksięgowania jest taka sama


                // 4. Aktualizuj salda
                fromAccount.Balance -= request.Amount;
                toAccount.Balance += amountToCredit; // amountToCredit to ta sama kwota, bo waluty są te same

                _context.Accounts.Update(fromAccount); // Jawne oznaczenie jako zmodyfikowane
                _context.Accounts.Update(toAccount);   // Jawne oznaczenie jako zmodyfikowane

                // 5. Utwórz i zapisz rekord transakcji
                var transaction = new Transaction
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountId = toAccount.Id,
                    Amount = request.Amount,          // Kwota pobrana z konta źródłowego
                    CurrencyCode = transactionCurrency, // Waluta konta źródłowego
                    Type = TransactionType.Transfer,
                    Timestamp = DateTime.UtcNow,
                    Description = request.Description
                };
                await _context.Transactions.AddAsync(transaction);

                // 6. Zapisz wszystkie zmiany w bazie danych
                await _context.SaveChangesAsync();

                // 7. Jeśli wszystko powyżej się udało, zatwierdź transakcję bazodanową
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
            catch (DbUpdateException ex) // Specyficzny wyjątek dla problemów z zapisem do bazy
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Database update error during transfer from AccountId {FromAccountId} to AccountId {ToAccountId}.", request.FromAccountId, request.ToAccountId);
                return new TransferResult { Success = false, ErrorMessage = "A database error occurred while processing the transfer." };
            }
            catch (Exception ex) // Ogólny handler dla innych nieoczekiwanych błędów
            {
                await dbTransaction.RollbackAsync(); // Zawsze próbuj wycofać transakcję w razie błędu
                _logger.LogError(ex, "An unexpected error occurred during the transfer from AccountId {FromAccountId} to AccountId {ToAccountId}.", request.FromAccountId, request.ToAccountId);
                return new TransferResult { Success = false, ErrorMessage = "An unexpected error occurred during the transfer." };
            }
        }
    }
}