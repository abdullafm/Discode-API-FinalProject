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
                .ForMember(d => d.DisplayName, o => o.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.DisplayName) ? s.UserName.Trim() : s.DisplayName.Trim()))
                .ForMember(d => d.BirthDay, o => o.MapFrom(s => s.BirthDay.Date))
                .ForMember(d => d.EmailConfirmed, o => o.MapFrom(_ => true));

            CreateMap<AppUser, LoginResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.IsSuccess, o => o.MapFrom(_ => true));
        }
    }
}
