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
            services.AddScoped<IFriendRequestService, FriendRequestService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IStoryService, StoryService>();
            services.AddScoped<ILiveStreamService, LiveStreamService>();
            services.AddScoped<IEmojiCategoryService, EmojiCategoryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IDirectMessageService, DirectMessageService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IMessageReactionService, MessageReactionService>();
            services.AddScoped<IPostLikeService, PostLikeService>();
            services.AddScoped<ICommentLikeService, CommentLikeService>();
            services.AddScoped<IStoryLikeService, StoryLikeService>();
            services.AddScoped<IStoryViewService, StoryViewService>();
            services.AddScoped<ILiveTrackerService, LiveTrackerService>();
            services.AddScoped<ICallService, CallService>();
            services.AddScoped<IUserBlockService, UserBlockService>();
            services.AddScoped<IPresenceService, PresenceService>();
            services.AddScoped<IPresenceTrackerService, PresenceTrackerService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            return services;
        }
    }
}
