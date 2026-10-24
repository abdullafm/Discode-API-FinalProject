using Service.Helpers.DTOs.Posts;

namespace Service.Services.Interfaces
{
    public interface ICommentService
    {
        Task<CommentDto> CreateAsync(string userId, int postId, CreateCommentDto model);
        Task<CommentDto> UpdateAsync(string userId, int commentId, UpdateTextDto model);
        Task DeleteAsync(string userId, int commentId);
        Task<LikeDto> ToggleLikeAsync(string userId, int commentId);
    }
}
