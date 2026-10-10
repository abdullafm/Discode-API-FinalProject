using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<AppUser> _userManager;

        public AccountRepository(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public Task<IdentityResult> RegisterAsync(AppUser user, string password)
            => _userManager.CreateAsync(user, password);

        public Task<IdentityResult> AddToRoleAsync(AppUser user, string role)
            => _userManager.AddToRoleAsync(user, role);

        public Task<AppUser?> GetByEmailAsync(string email)
            => _userManager.FindByEmailAsync(email);

        public Task<AppUser?> GetByUserNameAsync(string userName)
            => _userManager.FindByNameAsync(userName);

        public Task<bool> CheckPasswordAsync(AppUser user, string password)
            => _userManager.CheckPasswordAsync(user, password);

        public Task<IList<string>> GetRolesAsync(AppUser user)
            => _userManager.GetRolesAsync(user);

        public Task<bool> IsLockedOutAsync(AppUser user)
            => _userManager.IsLockedOutAsync(user);

        public Task RegisterFailedAttemptAsync(AppUser user)
            => _userManager.AccessFailedAsync(user);

        public Task ResetFailedAttemptsAsync(AppUser user)
            => _userManager.ResetAccessFailedCountAsync(user);
    }
}
