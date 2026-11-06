using Service.Helpers.DTOs.Friends;

namespace Service.Services.Interfaces
{
    public interface IUserIgnoreService
    {
        Task<IgnoreDto> ToggleAsync(string userId, IgnoreUserDto model);
        Task<IEnumerable<string>> GetIgnoredUserNamesAsync(string userId);
    }
}
