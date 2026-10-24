using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IPostRepostRepository : IBaseRepository<PostRepost>
    {
        Task<PostRepost?> GetByUserAsync(int postId, string userId);
        Task<int> CountAsync(int postId);
    }
}
