// Plik: Models/TransactionType.cs

namespace CurrencyTransferAPI.Models
{
    public enum TransactionType
    {
        Transfer,
        Deposit,
        Withdrawal,
        Exchange // POPRAWKA: Dodano brakujący typ transakcji
    }
}
