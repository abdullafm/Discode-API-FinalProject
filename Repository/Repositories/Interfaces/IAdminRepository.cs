namespace Repository.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        Task<int> CountUsersAsync();
        Task<int> CountUsersCreatedSinceAsync(DateTime since);
        Task<int> CountUsersByVisibilityAsync(bool isPublic);
        Task<int> CountUsersInRoleAsync(string roleName);
        Task<int> CountUserBlocksAsync();

        Task<int> CountServersAsync();
        Task<int> CountServersByPrivacyAsync(bool isPrivate);
        Task<int> CountServerMembershipsAsync();

        Task<int> CountServerBansAsync();
        Task<int> CountServerBansSinceAsync(DateTime since);
        Task<int> CountDistinctBannedUsersAsync();
        Task<int> CountServersWithBansAsync();
        Task<int> CountActiveTimeoutsAsync(DateTime now);

        Task<int> CountPostsAsync();
        Task<int> CountCommentsAsync();
        Task<int> CountRepostsAsync();
        Task<int> CountStoriesAsync();
        Task<int> CountGroupsAsync();
        Task<int> CountDirectMessagesAsync();
        Task<int> CountGroupMessagesAsync();
        Task<int> CountServerMessagesAsync();

        Task<int> CountReportsAsync();
        Task<int> CountReportsByStatusAsync(Domain.Enums.ReportStatus status);
        Task<List<(Domain.Enums.ReportReason Reason, int Count)>> GetTopReportReasonsAsync(int take);
    }
}
