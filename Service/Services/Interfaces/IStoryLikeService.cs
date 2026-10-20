using Service.Helpers.DTOs.Posts;

namespace Service.Services.Interfaces
{
    public interface IStoryLikeService
    {
        Task<LikeDto> ToggleAsync(int storyId, string userId);
        Task<HashSet<string>> GetLikedUserIdsAsync(int storyId);
    }
}
