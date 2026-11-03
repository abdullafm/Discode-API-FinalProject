using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public record ContentRemoval(string AuthorId, string Kind, string Snippet);

    public interface IAdminContentService
    {
        Task<AdminContentListDto> GetAsync(string? type, string? search, int page, int pageSize);
        Task<(string AuthorId, string Kind, string Snippet)> FindAsync(string type, int id);
        Task<ContentRemoval> DeleteAsync(ModerationActor actor, string type, int id, string reason);
    }
}
