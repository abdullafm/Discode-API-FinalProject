using Microsoft.Extensions.DependencyInjection;
using Repository.Repositories;
using Repository.Repositories.Interfaces;

namespace Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
        {
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
            services.AddScoped<IUserBlockRepository, UserBlockRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IStoryRepository, StoryRepository>();
            services.AddScoped<IStoryLikeRepository, StoryLikeRepository>();
            services.AddScoped<IStoryViewRepository, StoryViewRepository>();
            services.AddScoped<ILiveStreamRepository, LiveStreamRepository>();
            services.AddScoped<IEmojiCategoryRepository, EmojiCategoryRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
            services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
            services.AddScoped<IPollRepository, PollRepository>();
            services.AddScoped<ICallRepository, CallRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IGroupMessageRepository, GroupMessageRepository>();
            services.AddSingleton<ILiveTrackerRepository, LiveTrackerRepository>();
            services.AddScoped<IPostLikeRepository, PostLikeRepository>();
            services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
            services.AddScoped<IPresenceRepository, PresenceRepository>();
            services.AddSingleton<IPresenceTrackerRepository, PresenceTrackerRepository>();
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            return services;
        }
    }
}
