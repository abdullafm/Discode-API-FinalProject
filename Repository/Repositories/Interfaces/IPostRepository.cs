using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IPostRepository : IBaseRepository<Post>
    {
        Task<IEnumerable<Post>> GetExploreAsync(string viewerId);
        Task<IEnumerable<Post>> GetFriendsFeedAsync(string viewerId);
        Task<IEnumerable<Post>> GetByUserAsync(string viewerId, string authorId);
        Task<IEnumerable<Post>> GetRepostedByAsync(string viewerId, string userId);
        Task<bool> IsVisibleAsync(string viewerId, int postId);
    }
}
