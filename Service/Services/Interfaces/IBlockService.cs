using Service.Helpers.DTOs.Friends;

namespace Service.Services.Interfaces
{
    public interface IBlockService
    {
        Task<FriendRequestDto> BlockAsync(string blockerId, BlockUserDto model);
        Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId);
        Task UnblockAsync(string userId, int blockId);
    }
}
