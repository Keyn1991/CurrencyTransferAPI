using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis; // Potrzebne dla SuppressMessage lub required

namespace CurrencyTransferAPI.Models
{
    // UserRoles.cs pozostaje bez zmian

    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; } // Już jest nullable, więc OK

        [Required]
        public string Role { get; set; } = UserRoles.User; // Już jest inicjalizowane, więc OK

        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>(); // Już jest inicjalizowane
    }
}