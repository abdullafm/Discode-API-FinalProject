using Service.Helpers.DTOs.Nitro;

namespace Service.Services.Interfaces
{
    public interface INitroAccessService
    {
        Task<bool> HasNitroAsync(string userId);
        Task<HashSet<string>> GetNitroUserIdsAsync(IEnumerable<string> userIds);
        Task<NitroLimitsDto> GetLimitsAsync(string userId);
        Task EnsureCanSendAsync(string userId, string? text, IEnumerable<long> fileSizes);
    }
}
