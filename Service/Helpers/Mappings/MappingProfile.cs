using AutoMapper;
using Domain.Entities;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.DTOs.Presence;
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
