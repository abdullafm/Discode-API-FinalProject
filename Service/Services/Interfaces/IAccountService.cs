using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
        Task<RegisterResponse> VerifyEmailAsync(VerifyEmailDto model);
        Task<RegisterResponse> ResendCodeAsync(ResendCodeDto model);
        Task<RegisterResponse> ForgotPasswordAsync(ForgotPasswordDto model);
        Task<RegisterResponse> ResetPasswordAsync(ResetPasswordDto model);
        Task<LoginResponse> LoginAsync(LoginDto model);
        Task<ProfileDto> GetProfileAsync(string userId);
        Task<UpdateProfileResponse> UpdateProfileAsync(string userId, UpdateProfileDto model);
        Task<UpdateProfileResponse> ChangeUsernameAsync(string userId, ChangeUsernameDto model);
        Task<UpdateProfileResponse> ChangePasswordAsync(string userId, ChangePasswordDto model);
        Task<IEnumerable<UserSearchDto>> SearchUserAsync(string searchName, string currentUserId);
        Task<UserProfileDto> GetUserProfileAsync(string viewerId, string userName);
    }
}
