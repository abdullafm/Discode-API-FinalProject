using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class TranslationRepository : ITranslationRepository
    {
        private readonly AppDbContext _dbContext;

        public TranslationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Dictionary<string, string>> GetByLanguageAsync(string languageCode)
        {
            return await _dbContext.Translations
                .AsNoTracking()
                .Where(x => x.LanguageCode == languageCode)
                .ToDictionaryAsync(x => x.Key, x => x.Value);
        }

        public async Task<string?> GetUserLanguageAsync(string userId)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.Language)
                .FirstOrDefaultAsync();
        }

        public async Task SetUserLanguageAsync(string userId, string languageCode)
        {
            await _dbContext.Users
                .Where(x => x.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.Language, languageCode));
        }
    }
}
