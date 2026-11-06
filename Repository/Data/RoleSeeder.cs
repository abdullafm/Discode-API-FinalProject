using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Repository.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedSuperAdminAsync(UserManager<AppUser> userManager, string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;

            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user is null || await userManager.IsInRoleAsync(user, Roles.SuperAdmin)) return;

            await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        }
    }
}
