using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class PostRepostRepository : BaseRepository<PostRepost>, IPostRepostRepository
    {
        public PostRepostRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PostRepost?> GetByUserAsync(int postId, string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
        }

        public async Task<int> CountAsync(int postId)
        {
            return await _dbSet.CountAsync(x => x.PostId == postId);
        }
    }
}
