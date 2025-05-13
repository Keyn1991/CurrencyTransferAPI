using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis; // Potrzebne dla SuppressMessage lub required

namespace CurrencyTransferAPI.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Balance { get; set; }
    }
}