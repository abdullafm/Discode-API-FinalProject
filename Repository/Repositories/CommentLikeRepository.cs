using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CommentLikeRepository : BaseRepository<CommentLike>, ICommentLikeRepository
    {
        public CommentLikeRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<CommentLike?> GetByUserAsync(int commentId, string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId);
        }

        public async Task<int> CountAsync(int commentId)
        {
            return await _dbSet.CountAsync(x => x.CommentId == commentId);
        }
    }
}
