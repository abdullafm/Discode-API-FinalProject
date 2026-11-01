using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public record UserStatusRow(bool IsBanned, DateTime? BannedUntil, string BanReason, DateTime? ForcedLogoutAt, DateTime? TimeoutUntil, string TimeoutReason, bool IsDeleted);

    public interface IUserModerationRepository
    {
        Task<AppUser?> GetTrackedUserAsync(string userId);
        Task SaveChangesAsync();
        Task AddWarningAsync(UserWarning warning);
        Task AddActionAsync(AdminAction action);
        Task<int> CountWarningsAsync(string userId);
        Task<List<(UserWarning Warning, string IssuedBy)>> GetWarningsAsync(string userId, int take);
        Task<List<AdminAction>> GetActionsForTargetAsync(AdminTargetType type, string targetId, int take);
        Task<(List<AdminAction> Actions, int Total)> GetActionsAsync(string? search, int page, int pageSize);
        Task<int> CountReportsAgainstAsync(string userId);
        Task<int> CountPostsByAsync(string userId);
        Task<int> CountCommentsByAsync(string userId);
        Task<UserStatusRow?> GetStatusAsync(string userId);
        Task<List<UserWarning>> GetUnseenWarningsAsync(string userId);
        Task MarkWarningSeenAsync(string userId, int warningId);
    }
}
