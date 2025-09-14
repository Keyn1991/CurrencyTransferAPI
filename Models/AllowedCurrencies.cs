using System.Collections.Generic;
using System.Linq;

namespace CurrencyTransferAPI.Models
{
    public static class AllowedCurrencies
    {
        public const string PLN = "PLN";
        public const string USD = "USD";
        public const string EUR = "EUR";
        public const string UAH = "UAH";

        private static readonly HashSet<string> _set = new HashSet<string> { PLN, USD, EUR, UAH };

        public static bool IsAllowed(string currencyCode) =>
            !string.IsNullOrEmpty(currencyCode) && _set.Contains(currencyCode.ToUpper());

        public static IEnumerable<string> GetAll() => _set.ToList();
    }
}