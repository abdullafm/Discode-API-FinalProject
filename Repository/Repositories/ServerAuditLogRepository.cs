using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerAuditLogRepository : BaseRepository<ServerAuditLog>, IServerAuditLogRepository
    {
        public ServerAuditLogRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerAuditLog>> GetForServerAsync(int serverId, string? actorId, ServerAuditAction? action, int? beforeId, int take)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(x => x.Actor)
                .Where(x => x.ServerId == serverId);

            if (!string.IsNullOrWhiteSpace(actorId))
            {
                query = query.Where(x => x.ActorId == actorId);
            }

            if (action is not null)
            {
                query = query.Where(x => x.Action == action);
            }

            if (beforeId is not null)
            {
                query = query.Where(x => x.Id < beforeId);
            }

            return await query.OrderByDescending(x => x.Id).Take(take).ToListAsync();
        }
    }
}
