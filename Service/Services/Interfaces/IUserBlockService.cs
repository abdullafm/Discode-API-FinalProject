using Service.Helpers.DTOs.Friends;

namespace Service.Services.Interfaces
{
    public interface IUserBlockService
    {
        Task<FriendRequestDto> BlockAsync(string blockerId, BlockUserDto model);
        Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId);
        Task<FriendRequestDto> UnblockAsync(string userId, int blockId);
    }
}
