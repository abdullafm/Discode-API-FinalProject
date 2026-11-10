using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IChatReadMarkerRepository : IBaseRepository<ChatReadMarker>
    {
        Task<ChatReadMarker?> GetAsync(string userId, string kind, string key);
    }
}
