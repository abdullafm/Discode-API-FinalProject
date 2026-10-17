using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Repository.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<IdentityResult> RegisterAsync(AppUser user, string password);
        Task<IdentityResult> AddToRoleAsync(AppUser user, string role);
        Task<AppUser?> GetByIdAsync(string id);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<AppUser?> GetByUserNameAsync(string userName);
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task<IEnumerable<string>> GetRolesAsync(AppUser user);
        Task<bool> IsLockedOutAsync(AppUser user);
        Task RegisterFailedAttemptAsync(AppUser user);
        Task ResetFailedAttemptsAsync(AppUser user);
        Task<IdentityResult> UpdateAsync(AppUser user);
        Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(AppUser user);
        Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword);
        Task<IEnumerable<AppUser>> SearchUserAsync(string searchName);
        Task<(AppUser User, FriendRequest? Request)?> GetUserProfileAsync(string viewerId, string userName);
    }
}
