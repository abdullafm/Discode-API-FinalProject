using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class OwoRepository : IOwoRepository
    {
        private readonly AppDbContext _db;

        public OwoRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OwoProfile> GetOrCreateProfileAsync(string userId, long startingCash)
        {
            var profile = await _db.OwoProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile is not null)
            {
                return profile;
            }

            profile = new OwoProfile { UserId = userId, Cowoncy = startingCash };
            _db.OwoProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public Task<List<OwoAnimal>> GetAnimalsAsync(string userId)
        {
            return _db.OwoAnimals.Where(x => x.UserId == userId && x.Count > 0).ToListAsync();
        }

        public async Task AddAnimalsAsync(string userId, IEnumerable<string> keys)
        {
            var owned = await _db.OwoAnimals.Where(x => x.UserId == userId).ToListAsync();
            foreach (var group in keys.GroupBy(k => k))
            {
                var row = owned.FirstOrDefault(x => x.AnimalKey == group.Key);
                if (row is null)
                {
                    _db.OwoAnimals.Add(new OwoAnimal { UserId = userId, AnimalKey = group.Key, Count = group.Count() });
                }
                else
                {
                    row.Count += group.Count();
                }
            }

            await _db.SaveChangesAsync();
        }

        public Task<List<OwoProfile>> TopAsync(IEnumerable<string> userIds, int take)
        {
            var ids = userIds.ToList();
            return _db.OwoProfiles.AsNoTracking().Include(x => x.User).Where(x => ids.Contains(x.UserId)).OrderByDescending(x => x.Cowoncy).Take(take).ToListAsync();
        }

        public Task SaveAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}
