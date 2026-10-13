using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IFriendRequestRepository : IBaseRepository<FriendRequest>
    {
        Task<FriendRequest?> GetBetweenAsync(string userIdA, string userIdB);
        Task<IReadOnlyList<FriendRequest>> GetSentPendingAsync(string senderId);
        Task<IReadOnlyList<FriendRequest>> GetReceivedPendingAsync(string receiverId);
        Task<IReadOnlyList<FriendRequest>> GetAcceptedAsync(string userId);
    }
}
