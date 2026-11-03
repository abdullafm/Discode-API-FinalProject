using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public record AdminContentRow(
        int Id, int? PostId, string AuthorId, string AuthorUserName, string AuthorDisplayName, string AuthorAvatarUrl,
        string Text, string MediaUrl, DateTime CreatedAt, int Likes, int Comments, int Views, int OpenReports);

    public interface IAdminContentRepository
    {
        Task<(List<AdminContentRow> Items, int Total)> GetPagedAsync(ReportTargetType type, string? search, int page, int pageSize);
        Task<int> CountAsync(ReportTargetType type);
        Task<int> ResolveReportsAsync(ReportTargetType type, string targetId, string adminId);
    }
}
