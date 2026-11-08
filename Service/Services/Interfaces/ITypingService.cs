using Service.Helpers.DTOs.Presence;

namespace Service.Services.Interfaces
{
    public interface ITypingService
    {
        // null when the person may not write there (so nobody is told)
        Task<TypingDto?> ResolveAsync(string userId, string kind, string key);
    }
}
