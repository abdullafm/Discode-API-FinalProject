using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerTemplateRepository : BaseRepository<ServerTemplate>, IServerTemplateRepository
    {
        public ServerTemplateRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public Task<ServerTemplate?> GetForServerAsync(int serverId)
        {
            return _dbSet.Include(x => x.Server).FirstOrDefaultAsync(x => x.ServerId == serverId);
        }

        public Task<ServerTemplate?> GetByCodeAsync(string code)
        {
            return _dbSet.Include(x => x.Server).FirstOrDefaultAsync(x => x.Code == code);
        }

        public Task<bool> CodeTakenAsync(string code)
        {
            return _dbSet.AnyAsync(x => x.Code == code);
        }
    }
}
