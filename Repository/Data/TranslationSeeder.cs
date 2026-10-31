using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Repository.Data
{
    public static class TranslationSeeder
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            using var stream = typeof(TranslationSeeder).Assembly.GetManifestResourceStream("translations.json")
                ?? throw new InvalidOperationException("The translation seed file is missing.");
            var seed = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, string>>>(stream)
                ?? new Dictionary<string, Dictionary<string, string>>();

            var stored = await dbContext.Translations
                .Select(x => new { x.LanguageCode, x.Key })
                .ToListAsync();
            var existing = stored.Select(x => x.LanguageCode + "\n" + x.Key).ToHashSet();

            var missing = new List<Translation>();
            foreach (var language in seed)
            {
                foreach (var entry in language.Value)
                {
                    if (existing.Contains(language.Key + "\n" + entry.Key)) continue;
                    missing.Add(new Translation { LanguageCode = language.Key, Key = entry.Key, Value = entry.Value });
                }
            }

            if (missing.Count == 0) return;
            dbContext.Translations.AddRange(missing);
            await dbContext.SaveChangesAsync();
        }
    }
}
