using Service.Helpers.DTOs.Posts;
using Service.Helpers.DTOs.Stories;

namespace Service.Services.Interfaces
{
    public interface IStoryService
    {
        Task<StoryDto> CreateAsync(string userId, CreateStoryDto model);
        Task<IEnumerable<StoryDto>> GetAsync(string viewerId);
        Task DeleteAsync(string userId, int storyId);
        Task<LikeDto> ToggleLikeAsync(string userId, int storyId);
    }
}
