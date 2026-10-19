using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Repository.Data
{
    public static class EmojiSeeder
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            if (await dbContext.EmojiCategories.AnyAsync())
            {
                return;
            }

            using var stream = typeof(EmojiSeeder).Assembly.GetManifestResourceStream("emojis.json")
                ?? throw new InvalidOperationException("The emoji seed file is missing.");
            var seed = await JsonSerializer.DeserializeAsync<List<EmojiSeedCategory>>(stream) ?? new List<EmojiSeedCategory>();

            var categories = seed.Select((category, categoryIndex) => new EmojiCategory
            {
                Name = category.Name,
                Icon = category.Icon,
                SortOrder = categoryIndex,
                Emojis = category.Emojis.Select((emoji, emojiIndex) => new Emoji
                {
                    Character = emoji.Character,
                    Name = emoji.Name,
                    Shortcodes = emoji.Shortcodes,
                    SortOrder = emojiIndex
                }).ToList()
            });

            await dbContext.EmojiCategories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();
        }
    }
}
