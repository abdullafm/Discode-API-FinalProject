using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IUserModerationService
    {
        Task<AdminUserDetailDto> GetAsync(ModerationActor actor, string userId);
        Task<ModerationResultDto> WarnAsync(ModerationActor actor, string userId, WarnUserDto model);
        Task<ModerationResultDto> TimeoutAsync(ModerationActor actor, string userId, TimeoutUserDto model);
        Task<ModerationResultDto> ClearTimeoutAsync(ModerationActor actor, string userId);
        Task<ModerationResultDto> ChangeRoleAsync(ModerationActor actor, string userId, ChangeRoleDto model);
        Task<ModerationResultDto> LiveBanAsync(ModerationActor actor, string userId, LiveBanDto model);
        Task<ModerationResultDto> LiveUnbanAsync(ModerationActor actor, string userId);
        Task<ModerationResultDto> LogLiveEndAsync(ModerationActor actor, string userId, string reason, string streamTitle);
        Task<ModerationResultDto> KickAsync(ModerationActor actor, string userId, KickUserDto model);
        Task<ModerationResultDto> BanAsync(ModerationActor actor, string userId, BanUserDto model);
        Task<ModerationResultDto> UnbanAsync(ModerationActor actor, string userId);
        Task<ModerationResultDto> DeleteAsync(ModerationActor actor, string userId, DeleteUserDto model);
        Task<AdminActionListDto> GetActivityAsync(AdminListFilterDto filter);
        Task<List<UnseenWarningDto>> GetUnseenWarningsAsync(string userId);
        Task AcknowledgeWarningAsync(string userId, int warningId);
    }
}
