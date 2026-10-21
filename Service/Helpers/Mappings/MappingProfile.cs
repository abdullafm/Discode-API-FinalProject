using AutoMapper;
using Domain.Entities;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Emojis;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.DTOs.Live;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.DTOs.Presence;
using Service.Helpers.DTOs.Stories;
using Service.Helpers.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.Mappings
{
    public class MappingProfile : Profile
    {
        public const string ViewerIdKey = "viewerId";

        private static AppUser OtherParticipant(Conversation conversation, ResolutionContext context)
        {
            var viewer = context.Items.TryGetValue(ViewerIdKey, out var value) ? (string?)value : null;
            return conversation.UserAId == viewer ? conversation.UserB : conversation.UserA;
        }

        public MappingProfile()
        {
            CreateMap<RegisterDto, AppUser>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName.Trim()))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Trim()))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName.Trim()))
                .ForMember(d => d.BirthDay, o => o.MapFrom(s => s.BirthDay.Date))
                .ForMember(d => d.EmailConfirmed, o => o.MapFrom(_ => false));

            CreateMap<AppUser, LoginResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.IsSuccess, o => o.MapFrom(_ => true));

            CreateMap<AppUser, ProfileDto>();

            CreateMap<Message, MessageDto>()
                .ForMember(d => d.SenderUserName, o => o.MapFrom(s => s.Sender.UserName ?? string.Empty))
                .ForMember(d => d.SenderDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Sender.DisplayName)
                    ? s.Sender.UserName ?? string.Empty
                    : s.Sender.DisplayName))
                .ForMember(d => d.SenderAvatarUrl, o => o.MapFrom(s => s.Sender.AvatarUrl ?? string.Empty))
                .ForMember(d => d.Mine, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.SenderId == (string?)viewer))
                .ForMember(d => d.Forwarded, o => o.MapFrom(s => s.Forward != null))
                .ForMember(d => d.IsCall, o => o.MapFrom(s => s.Kind == Domain.Enums.MessageKind.Call))
                .ForMember(d => d.ReplyTo, o => o.MapFrom((s, d, m, context) => s.ReplyTo == null ? null : new ReplyDto
                {
                    MessageId = s.ReplyTo.Id,
                    SenderUserName = s.ReplyTo.Sender == null ? string.Empty : s.ReplyTo.Sender.UserName ?? string.Empty,
                    SenderDisplayName = s.ReplyTo.Sender == null ? string.Empty : (string.IsNullOrWhiteSpace(s.ReplyTo.Sender.DisplayName) ? s.ReplyTo.Sender.UserName ?? string.Empty : s.ReplyTo.Sender.DisplayName),
                    SenderAvatarUrl = s.ReplyTo.Sender == null ? string.Empty : s.ReplyTo.Sender.AvatarUrl ?? string.Empty,
                    Text = s.ReplyTo.Text.Length > 120 ? s.ReplyTo.Text.Substring(0, 120) : s.ReplyTo.Text
                }))
                .ForMember(d => d.Reactions, o => o.MapFrom((s, d, m, context) => s.Reactions
                    .OrderBy(r => r.CreatedAt)
                    .ThenBy(r => r.Id)
                    .GroupBy(r => r.Emoji)
                    .Select(g => new ReactionDto
                    {
                        Emoji = g.Key,
                        Count = g.Count(),
                        Reacted = context.Items.TryGetValue(ViewerIdKey, out var viewer) && g.Any(r => r.UserId == (string?)viewer)
                    })
                    .ToList()));

            CreateMap<Conversation, ConversationDto>()
                .ForMember(d => d.ConversationId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.UserId, o => o.MapFrom((s, d, m, context) => OtherParticipant(s, context).Id))
                .ForMember(d => d.UserName, o => o.MapFrom((s, d, m, context) => OtherParticipant(s, context).UserName ?? string.Empty))
                .ForMember(d => d.DisplayName, o => o.MapFrom((s, d, m, context) =>
                    string.IsNullOrWhiteSpace(OtherParticipant(s, context).DisplayName)
                        ? OtherParticipant(s, context).UserName ?? string.Empty
                        : OtherParticipant(s, context).DisplayName))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom((s, d, m, context) => OtherParticipant(s, context).AvatarUrl ?? string.Empty))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.LastMessage, o => o.MapFrom(s => s.LastMessageText))
                .ForMember(d => d.LastMessageMine, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.LastSenderId == (string?)viewer))
                .ForMember(d => d.UnreadCount, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.UserAId == (string?)viewer ? s.UserAUnread : s.UserBUnread));

            CreateMap<Notification, NotificationDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.ActorUserName, o => o.MapFrom(s => s.Actor.UserName ?? string.Empty))
                .ForMember(d => d.ActorDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Actor.DisplayName)
                    ? s.Actor.UserName ?? string.Empty
                    : s.Actor.DisplayName))
                .ForMember(d => d.ActorAvatarUrl, o => o.MapFrom(s => s.Actor.AvatarUrl ?? string.Empty))
                .ForMember(d => d.Snippet, o => o.MapFrom(s => s.Snippet ?? string.Empty));

            CreateMap<Emoji, EmojiDto>();
            CreateMap<EmojiCategory, EmojiCategoryDto>();

            CreateMap<LiveStream, LiveStreamDto>()
                .ForMember(d => d.HostUserName, o => o.MapFrom(s => s.Host.UserName ?? string.Empty))
                .ForMember(d => d.HostDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Host.DisplayName)
                    ? s.Host.UserName ?? string.Empty
                    : s.Host.DisplayName))
                .ForMember(d => d.HostAvatarUrl, o => o.MapFrom(s => s.Host.AvatarUrl ?? string.Empty))
                .ForMember(d => d.StartedAt, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.ThumbnailUrl, o => o.MapFrom(s => s.ThumbnailUrl ?? string.Empty))
                .ForMember(d => d.ViewerCount, o => o.Ignore())
                .ForMember(d => d.IsFriend, o => o.Ignore())
                .ForMember(d => d.IsMine, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.HostId == (string?)viewer));

            CreateMap<Story, StoryDto>()
                .ForMember(d => d.AuthorUserName, o => o.MapFrom(s => s.Author.UserName ?? string.Empty))
                .ForMember(d => d.AuthorDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Author.DisplayName)
                    ? s.Author.UserName ?? string.Empty
                    : s.Author.DisplayName))
                .ForMember(d => d.AuthorAvatarUrl, o => o.MapFrom(s => s.Author.AvatarUrl ?? string.Empty))
                .ForMember(d => d.LikeCount, o => o.MapFrom(s => s.Likes.Count))
                .ForMember(d => d.LikedByMe, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.Likes.Any(l => l.UserId == (string?)viewer)))
                .ForMember(d => d.ViewCount, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.AuthorId == (string?)viewer ? s.Views.Count : 0));

            CreateMap<StoryView, StoryViewerDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName ?? string.Empty))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.User.DisplayName)
                    ? s.User.UserName ?? string.Empty
                    : s.User.DisplayName))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.User.AvatarUrl ?? string.Empty))
                .ForMember(d => d.ViewedAt, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.Liked, o => o.Ignore());

            CreateMap<Comment, CommentDto>()
                .ForMember(d => d.AuthorUserName, o => o.MapFrom(s => s.Author.UserName ?? string.Empty))
                .ForMember(d => d.AuthorDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Author.DisplayName)
                    ? s.Author.UserName ?? string.Empty
                    : s.Author.DisplayName))
                .ForMember(d => d.AuthorAvatarUrl, o => o.MapFrom(s => s.Author.AvatarUrl ?? string.Empty))
                .ForMember(d => d.LikeCount, o => o.MapFrom(s => s.Likes.Count))
                .ForMember(d => d.LikedByMe, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.Likes.Any(l => l.UserId == (string?)viewer)));

            CreateMap<Post, PostDto>()
                .ForMember(d => d.LikeCount, o => o.MapFrom(s => s.Likes.Count))
                .ForMember(d => d.LikedByMe, o => o.MapFrom((s, d, m, context) =>
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.Likes.Any(l => l.UserId == (string?)viewer)))
                .ForMember(d => d.Comments, o => o.MapFrom(s => s.Comments.OrderBy(c => c.CreatedAt)))
                .ForMember(d => d.AuthorUserName, o => o.MapFrom(s => s.Author.UserName ?? string.Empty))
                .ForMember(d => d.AuthorDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Author.DisplayName)
                    ? s.Author.UserName ?? string.Empty
                    : s.Author.DisplayName))
                .ForMember(d => d.AuthorAvatarUrl, o => o.MapFrom(s => s.Author.AvatarUrl ?? string.Empty));

            CreateMap<AppUser, UserProfileDto>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName ?? string.Empty))
                .ForMember(d => d.MemberSince, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.IsPrivate, o => o.Ignore())
                .ForMember(d => d.Relation, o => o.Ignore())
                .ForMember(d => d.RequestId, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore());

            CreateMap<AppUser, UserSearchDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName ?? string.Empty))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl ?? string.Empty))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName ?? string.Empty));

            CreateMap<AppUser, PresenceChangeDto>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.PreferredStatus, o => o.MapFrom(s => PresenceStatusMap.ToWire(s.PreferredStatus)))
                .ForMember(d => d.ShouldBroadcast, o => o.Ignore())
                .ForMember(d => d.FriendIds, o => o.Ignore());

            CreateMap<AppUser, FriendRequestDto>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName ?? string.Empty))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.DisplayName)
                    ? s.UserName ?? string.Empty
                    : s.DisplayName))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl ?? string.Empty));
        }
    }
}
