// Plik: Models/Transaction.cs
// Poprawiona wersja - zawiera już tylko definicję klasy Transaction.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CurrencyTransferAPI.Models
{
    // Usunięto stąd zduplikowaną definicję 'enum TransactionType'.
    // Powinna ona znajdować się w swoim własnym, osobnym pliku 'TransactionType.cs'.

    public class Transaction
    {
        public int Id { get; set; }

        public int? FromAccountId { get; set; }
        public virtual Account? FromAccount { get; set; }

        [Required]
        public int ToAccountId { get; set; }
        public virtual Account ToAccount { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; } = TransactionType.Transfer;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
