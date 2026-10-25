using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerMemberRepository : BaseRepository<ServerMember>, IServerMemberRepository
    {
        public ServerMemberRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ServerMember?> GetAsync(int serverId, string userId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.UserId == userId);
        }
    }
}
