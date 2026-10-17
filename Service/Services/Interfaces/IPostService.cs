using Service.Helpers.DTOs.Posts;

namespace Service.Services.Interfaces
{
    public interface IPostService
    {
        Task<PostDto> CreateAsync(string userId, CreatePostDto model);
        Task<IEnumerable<PostDto>> GetExploreAsync(string viewerId);
        Task<IEnumerable<PostDto>> GetFriendsAsync(string viewerId);
        Task<IEnumerable<PostDto>> GetByUserAsync(string viewerId, string userName);
        Task DeleteAsync(string userId, int postId);
    }
}
