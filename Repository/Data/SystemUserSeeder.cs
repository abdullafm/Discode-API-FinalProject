using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data
{
    public static class SystemUserSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync(x => x.IsSystem)) return;

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
