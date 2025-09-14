using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [EmailAddress]
        public string? Email { get; set; }
        // Możesz dodać tu inne pola do aktualizacji w przyszłości
    }

    public interface IUserService
    {
        Task<UserProfileDto?> GetUserProfileByIdAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UpdateUserProfileDto dto);
        Task<bool> DeleteUserAsync(int userId);
    }
}
