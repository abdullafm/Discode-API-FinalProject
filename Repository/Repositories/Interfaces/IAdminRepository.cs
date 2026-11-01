namespace Repository.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        Task<int> CountUsersAsync();
        Task<int> CountUsersCreatedSinceAsync(DateTime since);
        Task<int> CountUsersByVisibilityAsync(bool isPublic);
        Task<int> CountUsersInRoleAsync(string roleName);

        Task<int> CountServersAsync();
        Task<int> CountServersByPrivacyAsync(bool isPrivate);

        Task<int> CountServerBansAsync();
        Task<int> CountServerBansSinceAsync(DateTime since);
        Task<int> CountDistinctBannedUsersAsync();
        Task<int> CountServersWithBansAsync();
        Task<int> CountActiveTimeoutsAsync(DateTime now);

        Task<int> CountPostsAsync();
        Task<int> CountCommentsAsync();

        Task<(List<Domain.Entities.AppUser> Users, int Total)> GetUsersAsync(string? search, string? role, bool bannedOnly, int page, int pageSize);
        Task<Dictionary<string, List<string>>> GetRolesForUsersAsync(List<string> userIds);
        Task<Dictionary<string, int>> GetServerCountsForUsersAsync(List<string> userIds);
        Task<Dictionary<string, int>> GetBanCountsForUsersAsync(List<string> userIds);
        Task<(List<Domain.Entities.Server> Servers, int Total)> GetAllServersAsync(bool bannedOnly, int page, int pageSize);
        Task<(List<Domain.Entities.Server> Servers, int Total)> SearchServersAsync(string term, bool bannedOnly, int page, int pageSize);
        Task<Dictionary<int, int>> GetMemberCountsForServersAsync(List<int> serverIds);
        Task<Domain.Entities.Server?> GetServerAsync(int serverId);
        Task<string?> GetUserNameAsync(string userId);
        Task<int> CountChannelsAsync(int serverId, Domain.Enums.ServerChannelKind kind);
        Task<int> CountRolesAsync(int serverId);
        Task<int> CountMessagesInServerAsync(int serverId);
        Task<List<Domain.Entities.ServerMember>> GetRecentMembersAsync(int serverId, int take);
        Task<int> CountBannedServersAsync();
        Task SetServerBanAsync(int serverId, bool banned, string reason, string? bannedById);
        Task<Dictionary<int, int>> GetBanCountsForServersAsync(List<int> serverIds);

        Task<int> CountReportsAsync();
        Task<int> CountReportsByStatusAsync(Domain.Enums.ReportStatus status);
        Task<List<(Domain.Enums.ReportReason Reason, int Count)>> GetTopReportReasonsAsync(int take);
    }
}
