using Service.Helpers.DTOs.Friends;

namespace Service.Services.Interfaces
{
    public interface IFriendRequestService
    {
        Task<FriendRequestDto> SendAsync(string senderId, SendFriendRequestDto model);
        Task<FriendPendingDto> GetPendingAsync(string userId);
        Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId);
        Task<string> CancelAsync(string userId, int requestId);
        Task<string> AcceptAsync(string userId, int requestId);
        Task<string> DeclineAsync(string userId, int requestId);
        Task<string> RemoveAsync(string userId, int requestId);
    }
}
