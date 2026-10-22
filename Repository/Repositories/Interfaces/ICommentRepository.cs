using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICommentRepository : IBaseRepository<Comment>
    {
        Task<List<Comment>> GetThreadAsync(int rootCommentId);
    }
}
