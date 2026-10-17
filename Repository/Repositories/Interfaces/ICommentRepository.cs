using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment> CreateAsync(Comment comment);
        Task<Comment?> GetByIdAsync(int id);
        Task DeleteAsync(Comment comment);
    }
}
