using Service.Helpers.DTOs.Stories;

namespace Service.Services.Interfaces
{
    public interface IStoryViewService
    {
        Task<bool> HasViewedAsync(int storyId, string userId);
        Task AddAsync(int storyId, string userId);
        Task<IEnumerable<StoryViewerDto>> GetViewersAsync(int storyId, ISet<string> likedUserIds);
    }
}
