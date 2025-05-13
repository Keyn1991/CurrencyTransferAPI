using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CurrencyTransferAPI.Models
{
    public enum TransactionType
    {
        Transfer,
        Deposit,
        Withdrawal,
        Exchange // NOWY TYP
    }

    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public int FromAccountId { get; set; }
        public virtual Account? FromAccount { get; set; }

        [Required]
        public int ToAccountId { get; set; }
        public virtual Account? ToAccount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = string.Empty; // Inicjalizacja

        public TransactionType Type { get; set; } = TransactionType.Transfer;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}