using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ILiveStreamRepository : IBaseRepository<LiveStream>
    {
        Task<IEnumerable<LiveStream>> GetActiveVisibleAsync(string viewerId, bool staff = false);
        Task<IEnumerable<LiveStream>> GetActiveAllAsync();
        Task<IEnumerable<LiveStream>> GetActiveByHostAsync(string hostId);
        Task<LiveStream?> GetActiveVisibleByIdAsync(string viewerId, int streamId, bool staff = false);
    }
}
