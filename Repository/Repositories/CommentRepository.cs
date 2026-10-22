using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CommentRepository : BaseRepository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<Comment>> GetThreadAsync(int rootCommentId)
        {
            var root = await _dbSet.FirstOrDefaultAsync(x => x.Id == rootCommentId);
            if (root is null)
            {
                return new List<Comment>();
            }

            var all = await _dbSet.Where(x => x.PostId == root.PostId).ToListAsync();
            var thread = new List<Comment> { root };
            for (var index = 0; index < thread.Count; index++)
            {
                var id = thread[index].Id;
                thread.AddRange(all.Where(x => x.ParentCommentId == id));
            }

            thread.Reverse();
            return thread;
        }
    }
}
