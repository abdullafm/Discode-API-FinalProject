using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.Mappings;
using Service.Helpers.Validators.Accounts;
using Service.Services;
using Service.Services.Interfaces;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IFriendService, FriendService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IStoryService, StoryService>();
            services.AddScoped<ILiveService, LiveService>();
            services.AddScoped<IEmojiService, EmojiService>();
            services.AddScoped<IBlockService, BlockService>();
            services.AddScoped<IPresenceService, PresenceService>();
            services.AddScoped<IPresenceTrackerService, PresenceTrackerService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            return services;
        }
    }
}
