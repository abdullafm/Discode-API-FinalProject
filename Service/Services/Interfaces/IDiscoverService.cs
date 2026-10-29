using Service.Helpers.DTOs.Discover;

namespace Service.Services.Interfaces
{
    public interface IDiscoverService
    {
        Task<IEnumerable<DiscoverServerDto>> GetAsync(DiscoverFilterDto filter);
        Task<DiscoverServerDto?> GetItemAsync(int serverId);
        Task<IEnumerable<DiscoverServerDto>> GetForUserAsync(string userId);
    }
}
