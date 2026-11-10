using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data
{
    public static class SystemUserSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedDiscodeAsync(db);
            await SeedOwoAsync(db);
        }

        private static async Task SeedOwoAsync(AppDbContext db)
        {
            var normalized = OwoBot.UserName.ToUpperInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(x => x.NormalizedUserName == normalized);
            if (existing is not null)
            {
                if (!existing.IsSystem)
                {
                    existing.IsSystem = true;
                    await db.SaveChangesAsync();
                }

                return;
            }

            db.Users.Add(new AppUser
            {
                UserName = OwoBot.UserName,
                NormalizedUserName = normalized,
                Email = "owo@discode.local",
                NormalizedEmail = "OWO@DISCODE.LOCAL",
                EmailConfirmed = true,
                DisplayName = OwoBot.DisplayName,
                AvatarUrl = OwoBot.AvatarUrl,
                Bio = "The OwO game. Type owo help in a server channel.",
                BirthDay = new DateTime(2020, 1, 1),
                IsSystem = true,
                IsPublic = true,
                ProfileSetupDone = true,
                SecurityStamp = Guid.NewGuid().ToString("N")
            });
            await db.SaveChangesAsync();
        }

        private static async Task SeedDiscodeAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync(x => x.NormalizedUserName == SystemUser.UserName.ToUpperInvariant() && x.IsSystem)) return;

            var normalized = SystemUser.UserName.ToUpperInvariant();
            var taken = await db.Users.FirstOrDefaultAsync(x => x.NormalizedUserName == normalized);
            if (taken is not null)
            {
                taken.IsSystem = true;
                await db.SaveChangesAsync();
                return;
            }

            db.Users.Add(new AppUser
            {
                UserName = SystemUser.UserName,
                NormalizedUserName = normalized,
                Email = "system@discode.local",
                NormalizedEmail = "SYSTEM@DISCODE.LOCAL",
                EmailConfirmed = true,
                DisplayName = SystemUser.DisplayName,
                Bio = "Official notices from the Discode team.",
                BirthDay = new DateTime(2020, 1, 1),
                IsSystem = true,
                IsPublic = true,
                SecurityStamp = Guid.NewGuid().ToString("N")
            });
            await db.SaveChangesAsync();
        }
    }
}
