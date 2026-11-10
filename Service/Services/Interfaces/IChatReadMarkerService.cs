using Service.Helpers.DTOs.ReadMarkers;

namespace Service.Services.Interfaces
{
    public interface IChatReadMarkerService
    {
        Task<ReadMarkerDto> GetAsync(string userId, string kind, string key);
        Task<ReadMarkerDto> MarkAsync(string userId, MarkReadDto model);
    }
}
