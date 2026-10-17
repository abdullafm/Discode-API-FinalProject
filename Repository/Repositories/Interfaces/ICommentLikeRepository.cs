using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICommentLikeRepository : IBaseRepository<CommentLike>
    {
        Task<CommentLike?> GetByUserAsync(int commentId, string userId);
        Task<int> CountAsync(int commentId);
    }
}
