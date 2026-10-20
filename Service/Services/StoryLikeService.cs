using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class StoryLikeService : IStoryLikeService
    {
        private readonly IStoryLikeRepository _likes;

        public StoryLikeService(IStoryLikeRepository likes)
        {
            _likes = likes;
        }

        public async Task<LikeDto> ToggleAsync(int storyId, string userId)
        {
            var like = await _likes.GetByUserAsync(storyId, userId);
            if (like is null)
            {
                await _likes.AddAsync(new StoryLike { StoryId = storyId, UserId = userId });
            }
            else
            {
                await _likes.DeleteAsync(like);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _likes.CountAsync(storyId)
            };
        }

        public async Task<HashSet<string>> GetLikedUserIdsAsync(int storyId)
        {
            return (await _likes.GetByStoryAsync(storyId)).Select(x => x.UserId).ToHashSet();
        }
    }
}
