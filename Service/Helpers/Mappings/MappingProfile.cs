using AutoMapper;
using Domain.Entities;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Friends;
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

            CreateMap<LiveStream, LiveStreamDto>()
                .ForMember(d => d.HostUserName, o => o.MapFrom(s => s.Host.UserName ?? string.Empty))
                .ForMember(d => d.HostDisplayName, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Host.DisplayName)
                    ? s.Host.UserName ?? string.Empty
                    : s.Host.DisplayName))
                .ForMember(d => d.HostAvatarUrl, o => o.MapFrom(s => s.Host.AvatarUrl ?? string.Empty))
                .ForMember(d => d.StartedAt, o => o.MapFrom(s => s.CreatedAt))
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
                    context.Items.TryGetValue(ViewerIdKey, out var viewer) && s.Likes.Any(l => l.UserId == (string?)viewer)));

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
                .ForMember(d => d.RequestId, o => o.Ignore());

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
