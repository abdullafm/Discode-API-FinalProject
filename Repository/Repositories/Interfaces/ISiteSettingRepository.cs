using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ISiteSettingRepository
    {
        Task<List<SiteSetting>> GetAllAsync();
        Task SetAsync(string key, string value);
        Task RemoveAsync(string key);
    }
}
