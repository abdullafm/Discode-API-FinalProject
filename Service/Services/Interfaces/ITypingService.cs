using Service.Helpers.DTOs.Presence;

namespace Service.Services.Interfaces
{
    public interface ITypingService
    {
        Task<TypingDto?> ResolveAsync(string userId, string kind, string key);
    }
}
