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
        Task<IList<string>> GetRolesAsync(AppUser user);
        Task<bool> IsLockedOutAsync(AppUser user);
        Task RegisterFailedAttemptAsync(AppUser user);
        Task ResetFailedAttemptsAsync(AppUser user);
    }
}
