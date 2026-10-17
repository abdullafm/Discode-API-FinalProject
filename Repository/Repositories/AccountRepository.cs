using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;

        public AccountRepository(UserManager<AppUser> userManager, AppDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public Task<IdentityResult> RegisterAsync(AppUser user, string password)
        {
            return _userManager.CreateAsync(user, password);
        }

        public Task<IdentityResult> AddToRoleAsync(AppUser user, string role)
        {
            return _userManager.AddToRoleAsync(user, role);
        }

        public Task<AppUser?> GetByIdAsync(string id)
        {
            return _userManager.FindByIdAsync(id);
        }

        public Task<AppUser?> GetByEmailAsync(string email)
        {
            return _userManager.FindByEmailAsync(email);
        }

        public Task<AppUser?> GetByUserNameAsync(string userName)
        {
            return _userManager.FindByNameAsync(userName);
        }

        public Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IEnumerable<string>> GetRolesAsync(AppUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public Task<bool> IsLockedOutAsync(AppUser user)
        {
            return _userManager.IsLockedOutAsync(user);
        }

        public async Task RegisterFailedAttemptAsync(AppUser user)
        {
            await _userManager.AccessFailedAsync(user);
        }

        public async Task ResetFailedAttemptsAsync(AppUser user)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user)
        {
            return _userManager.UpdateAsync(user);
        }

        public Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
        {
            return _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public Task<string> GeneratePasswordResetTokenAsync(AppUser user)
        {
            return _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword)
        {
            return _userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task<IEnumerable<AppUser>> SearchUserAsync(string searchName)
        {
            var term = searchName.Trim();
            return await _userManager.Users
                .Where(n => (n.UserName != null && n.UserName.Contains(term)) || n.DisplayName.Contains(term)).ToListAsync();
        }

        public async Task<(AppUser User, FriendRequest? Request)?> GetUserProfileAsync(string viewerId, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user is null)
            {
                return null;
            }

            var blockedViewer = await _dbContext.UserBlocks
                .AnyAsync(x => x.BlockerId == user.Id && x.BlockedId == viewerId);
            if (blockedViewer)
            {
                return null;
            }

            var request = await _dbContext.FriendRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    (x.SenderId == viewerId && x.ReceiverId == user.Id) ||
                    (x.SenderId == user.Id && x.ReceiverId == viewerId));

            return (user, request);
        }
    }
}
