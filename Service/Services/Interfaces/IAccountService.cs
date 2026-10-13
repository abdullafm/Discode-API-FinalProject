using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
        Task<RegisterResponse> VerifyEmailAsync(VerifyEmailDto model);
        Task<RegisterResponse> ResendCodeAsync(ResendCodeDto model);
        Task<LoginResponse> LoginAsync(LoginDto model);
        Task<ProfileDto?> GetProfileAsync(string userId);
        Task<UpdateProfileResponse> UpdateProfileAsync(string userId, UpdateProfileDto model);
        Task<IEnumerable<UserSearchDto>> SearchUserAsync(string searchName, string currentUserId);
    }
}
