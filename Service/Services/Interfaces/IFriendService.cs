using Service.Helpers.DTOs.Friends;

namespace Service.Services.Interfaces
{
    public interface IFriendService
    {
        Task<FriendRequestDto> SendAsync(string senderId, SendFriendRequestDto model);
        Task<FriendPendingDto> GetPendingAsync(string userId);
        Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId);
        Task CancelAsync(string userId, int requestId);
        Task AcceptAsync(string userId, int requestId);
        Task DeclineAsync(string userId, int requestId);
        Task RemoveAsync(string userId, int requestId);
    }
}
