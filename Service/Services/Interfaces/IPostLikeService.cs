using Service.Helpers.DTOs.Posts;

namespace Service.Services.Interfaces
{
    public interface IPostLikeService
    {
        Task<LikeDto> ToggleAsync(int postId, string userId);
    }
}
