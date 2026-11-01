using Microsoft.Extensions.Caching.Memory;
using Repository.Repositories.Interfaces;

namespace Discode_API_FinalProject.Filters
{
    // keeps the ban / timeout / forced-logout state of a signed-in user for a few seconds so every request does not hit the database
    public class AccountStatusCache
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(5);
        private readonly IMemoryCache _cache;

        public AccountStatusCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<UserStatusRow?> GetAsync(string userId, IUserModerationRepository repository)
        {
            if (_cache.TryGetValue(Key(userId), out UserStatusRow? cached))
            {
                return cached;
            }

            var status = await repository.GetStatusAsync(userId);
            _cache.Set(Key(userId), status, Lifetime);
            return status;
        }

        public void Invalidate(string userId)
        {
            _cache.Remove(Key(userId));
        }

        private static string Key(string userId) => "account-status:" + userId;
    }
}
