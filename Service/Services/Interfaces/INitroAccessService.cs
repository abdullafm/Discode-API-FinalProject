using Service.Helpers.DTOs.Nitro;

namespace Service.Services.Interfaces
{
    // Read only: does this person have Nitro, and what may they do because of it
    public interface INitroAccessService
    {
        Task<bool> HasNitroAsync(string userId);
        Task<HashSet<string>> GetNitroUserIdsAsync(IEnumerable<string> userIds);
        Task<NitroLimitsDto> GetLimitsAsync(string userId);
        // refuses a message whose text or files are bigger than this person's account allows (Nitro allows more)
        Task EnsureCanSendAsync(string userId, string? text, IEnumerable<long> fileSizes);
    }
}
