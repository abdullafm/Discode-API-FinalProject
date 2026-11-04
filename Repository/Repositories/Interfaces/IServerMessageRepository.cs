using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerMessageRepository : IBaseRepository<ServerMessage>
    {
        Task<IEnumerable<ServerMessage>> GetLatestAsync(int channelId, int count, int? beforeId = null);
        Task<ServerMessage?> GetWithSenderAsync(int id);
        Task<List<ServerMessage>> GetPinnedAsync(int channelId);
        Task<int> CountPinnedAsync(int channelId);
        Task ClearRepliesAsync(int messageId);
        Task DeleteForChannelsAsync(IReadOnlyCollection<int> channelIds);
        Task ToggleReactionAsync(int messageId, string userId, string emoji);
    }
}
