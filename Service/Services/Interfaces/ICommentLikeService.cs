using Service.Helpers.DTOs.Posts;

namespace Service.Services.Interfaces
{
    public interface ICommentLikeService
    {
        Task<LikeDto> ToggleAsync(int commentId, string userId);
    }
}
