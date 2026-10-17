using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IPostLikeRepository : IBaseRepository<PostLike>
    {
        Task<PostLike?> GetByUserAsync(int postId, string userId);
        Task<int> CountAsync(int postId);
    }
}
