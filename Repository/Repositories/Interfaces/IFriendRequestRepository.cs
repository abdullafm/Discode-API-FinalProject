using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IFriendRequestRepository : IBaseRepository<FriendRequest>
    {
        Task<FriendRequest?> GetBetweenAsync(string userIdA, string userIdB);
        Task<IEnumerable<FriendRequest>> GetSentPendingAsync(string senderId);
        Task<IEnumerable<FriendRequest>> GetReceivedPendingAsync(string receiverId);
        Task<IEnumerable<FriendRequest>> GetAcceptedAsync(string userId);
    }
}
