using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CommentLikeService : ICommentLikeService
    {
        private readonly ICommentLikeRepository _likes;

        public CommentLikeService(ICommentLikeRepository likes)
        {
            _likes = likes;
        }

        public async Task<LikeDto> ToggleAsync(int commentId, string userId)
        {
            var like = await _likes.GetByUserAsync(commentId, userId);
            if (like is null)
            {
                await _likes.AddAsync(new CommentLike { CommentId = commentId, UserId = userId });
            }
            else
            {
                await _likes.DeleteAsync(like);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _likes.CountAsync(commentId)
            };
        }
    }
}
