using Domain.Common;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Nitro;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class NitroAccessService : INitroAccessService
    {
        private readonly INitroSubscriptionRepository _subscriptions;

        public NitroAccessService(INitroSubscriptionRepository subscriptions)
        {
            _subscriptions = subscriptions;
        }

        public async Task<bool> HasNitroAsync(string userId)
        {
            var found = await _subscriptions.GetByUserAsync(userId);
            return found is not null && found.IsActive(DateTime.UtcNow);
        }

        public Task<HashSet<string>> GetNitroUserIdsAsync(IEnumerable<string> userIds)
        {
            return _subscriptions.GetActiveUserIdsAsync(userIds, DateTime.UtcNow);
        }

        public async Task<NitroLimitsDto> GetLimitsAsync(string userId)
        {
            var has = await HasNitroAsync(userId);
            return new NitroLimitsDto
            {
                HasNitro = has,
                MaxMessageLength = has ? NitroRules.NitroMessageLength : NitroRules.FreeMessageLength,
                MaxFileBytes = has ? NitroRules.NitroFileBytes : NitroRules.FreeFileBytes
            };
        }

        public async Task EnsureCanSendAsync(string userId, string? text, IEnumerable<long> fileSizes)
        {
            var length = text?.Trim().Length ?? 0;
            var biggest = fileSizes.DefaultIfEmpty(0).Max();
            if (length <= NitroRules.FreeMessageLength && biggest <= NitroRules.FreeFileBytes)
            {
                return;
            }

            var limits = await GetLimitsAsync(userId);
            if (length > limits.MaxMessageLength)
            {
                throw new BadRequestException(limits.HasNitro
                    ? "The message can be at most " + limits.MaxMessageLength + " characters."
                    : "The message can be at most " + limits.MaxMessageLength + " characters. Nitro lets you write much longer ones.");
            }

            if (biggest > limits.MaxFileBytes)
            {
                throw new BadRequestException(limits.HasNitro
                    ? "A file can be at most 2 GB."
                    : "A file can be at most 5 MB. Nitro lets you send files up to 2 GB.");
            }
        }
    }
}
