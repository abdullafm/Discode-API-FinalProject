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
            services.AddScoped<IUserIgnoreRepository, UserIgnoreRepository>();
            services.AddScoped<IServerRepository, ServerRepository>();
            services.AddScoped<IServerMemberRepository, ServerMemberRepository>();
            services.AddScoped<IServerRoleRepository, ServerRoleRepository>();
            services.AddScoped<IServerAuditLogRepository, ServerAuditLogRepository>();
            services.AddScoped<IServerMemberRoleRepository, ServerMemberRoleRepository>();
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
            services.AddScoped<IGroupCallRepository, GroupCallRepository>();
            services.AddScoped<IGroupMessageRepository, GroupMessageRepository>();
            services.AddSingleton<ILiveTrackerRepository, LiveTrackerRepository>();
            services.AddSingleton<IGroupCallRingRepository, GroupCallRingRepository>();
            services.AddScoped<IPostLikeRepository, PostLikeRepository>();
            services.AddScoped<IPostRepostRepository, PostRepostRepository>();
            services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
            services.AddScoped<IPresenceRepository, PresenceRepository>();
            services.AddSingleton<IPresenceTrackerRepository, PresenceTrackerRepository>();
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            return services;
        }
    }
}
