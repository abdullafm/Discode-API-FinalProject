using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class PostLikeService : IPostLikeService
    {
        private readonly IPostLikeRepository _likes;

        public PostLikeService(IPostLikeRepository likes)
        {
            _likes = likes;
        }

        public async Task<LikeDto> ToggleAsync(int postId, string userId)
        {
            var like = await _likes.GetByUserAsync(postId, userId);
            if (like is null)
            {
                await _likes.AddAsync(new PostLike { PostId = postId, UserId = userId });
            }
            else
            {
                await _likes.DeleteAsync(like);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _likes.CountAsync(postId)
            };
        }
    }
}
