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
            services.AddScoped<IConversationPinService, ConversationPinService>();
            services.AddScoped<INitroAccessService, NitroAccessService>();
            services.AddScoped<IChatReadMarkerService, ChatReadMarkerService>();
            services.AddScoped<ITypingService, TypingService>();
            services.AddScoped<IBoostService, BoostService>();
            services.AddScoped<INitroService, NitroService>();
            services.AddScoped<IStripeGateway, StripeGateway>();
            services.AddScoped<ILiveStreamService, LiveStreamService>();
            services.AddScoped<ILiveModerationService, LiveModerationService>();
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
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IMentionService, MentionService>();
            services.AddScoped<IGroupCallService, GroupCallService>();
            services.AddScoped<IUserBlockService, UserBlockService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAdminReportService, AdminReportService>();
            services.AddScoped<IAdminContentService, AdminContentService>();
            services.AddScoped<IUserIgnoreService, UserIgnoreService>();
            services.AddScoped<IServerService, ServerService>();
            services.AddScoped<IServerPermissionService, ServerPermissionService>();
            services.AddScoped<IServerRoleService, ServerRoleService>();
            services.AddScoped<IServerAuditService, ServerAuditService>();
            services.AddScoped<IServerBanService, ServerBanService>();
            services.AddScoped<IServerPruneService, ServerPruneService>();
            services.AddScoped<IServerTemplateService, ServerTemplateService>();
            services.AddScoped<IOwoService, OwoService>();
            services.AddScoped<IGroupShareService, GroupShareService>();
            services.AddScoped<IDiscoverService, DiscoverService>();
            services.AddScoped<ITranslationService, TranslationService>();
            services.AddScoped<IBrandingService, BrandingService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IUserModerationService, UserModerationService>();
            services.AddScoped<IServerChannelService, ServerChannelService>();
            services.AddScoped<IServerTimeoutService, ServerTimeoutService>();
            services.AddScoped<IServerVoiceService, ServerVoiceService>();
            services.AddScoped<IServerMessageService, ServerMessageService>();
            services.AddScoped<IPresenceService, PresenceService>();
            services.AddScoped<IPresenceTrackerService, PresenceTrackerService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            return services;
        }
    }
}
