using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repository;
using Repository.Data;
using Repository.Notifications;
using Repository.Repositories.Interfaces;
using Service;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class TestHost : IDisposable
    {
        public const string Password = "Test.123!";

        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public TestEmail Mail { get; } = new();
        public FakeStripeGateway Stripe { get; } = new();

        public TestHost()
        {
            var services = new ServiceCollection();
            var database = Guid.NewGuid().ToString();

            services.AddLogging(logging => logging.ClearProviders());
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddSingleton<IAdminChangeNotifier, SilentAdminNotifier>();
            services.AddSingleton<IEmailRepository>(Mail);
            services.AddDbContext<AppDbContext>((provider, options) =>
                options.UseInMemoryDatabase(database)
                       .AddInterceptors(provider.GetRequiredService<Repository.Interceptors.AdminChangeInterceptor>()));

            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<JwtSettings>(settings =>
            {
                settings.Key = "DiscodeAPI-SecureKey-2024-MinimumThirtyTwoCharactersRequired!!!";
                settings.Issuer = "DiscodeAPI";
                settings.Audience = "DiscodeClient";
                settings.ExpiryMinutes = 1440;
            });
            services.Configure<AppSettings>(settings => settings.ClientUrl = "https://localhost:7208");

            services.AddServiceLayer();
            services.AddRepositoryLayer();
            services.AddSingleton<Service.Services.Interfaces.IStripeGateway>(Stripe);

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();
            SeedAsync().GetAwaiter().GetResult();
        }

        public T Get<T>() where T : notnull
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public async Task<AppUser> AddUserAsync(string role, string email)
        {
            var users = Get<UserManager<AppUser>>();
            var local = email.Split('@')[0];
            var userName = new string(local.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_').ToArray());
            if (userName.Length < 2)
            {
                userName = "user" + Guid.NewGuid().ToString("N")[..8];
            }

            var user = new AppUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                DisplayName = userName.Length <= 32 ? userName : userName[..32],
                BirthDay = new DateTime(2000, 1, 1),
                IsPublic = true
            };

            var created = await users.CreateAsync(user, Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", created.Errors.Select(error => error.Description)));
            }

            var assigned = await users.AddToRoleAsync(user, role);
            if (!assigned.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", assigned.Errors.Select(error => error.Description)));
            }

            return user;
        }

        public async Task MakeFriendsAsync(AppUser left, AppUser right)
        {
            var friends = Get<IFriendRequestService>();
            var request = await friends.SendAsync(left.Id, new SendFriendRequestDto { UserName = right.UserName });
            await friends.AcceptAsync(right.Id, request.Id);
        }

        public Task<ServerChangeDto> CreateServerAsync(AppUser owner, string name = "Nexus")
        {
            return Get<IServerService>().CreateAsync(owner.Id, new CreateServerDto { Name = name });
        }

        public static CreatePollDto Poll(string question = "Lunch?")
        {
            return new CreatePollDto
            {
                Question = question,
                DurationHours = 24,
                Options =
                {
                    new CreatePollOptionDto { Text = "Yes" },
                    new CreatePollOptionDto { Text = "No" }
                }
            };
        }

        public static ModerationActor Super(AppUser admin) => new() { UserId = admin.Id, IsSuperAdmin = true };

        public static ModerationActor Staff(AppUser admin) => new() { UserId = admin.Id, IsSuperAdmin = false };

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task SeedAsync()
        {
            var db = Get<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await RoleSeeder.SeedAsync(Get<RoleManager<IdentityRole>>());
            await SystemUserSeeder.SeedAsync(db);
        }
    }
}
