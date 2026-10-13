using AutoMapper;
using Domain.Entities;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

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

            CreateMap<UpdateProfileDto, AppUser>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName.Trim()))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName.Trim()))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl.Trim()))
                .ForMember(d => d.CustomStatus, o => o.MapFrom(s => s.CustomStatus.Trim()))
                .ForMember(d => d.Pronouns, o => o.MapFrom(s => s.Pronouns.Trim()))
                .ForMember(d => d.Bio, o => o.MapFrom(s => s.Bio.Trim()));

            CreateMap<AppUser, UserSearchDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName ?? string.Empty))
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl ?? string.Empty))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName ?? string.Empty));
        }
    }
}
