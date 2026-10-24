namespace Repository.Repositories.Interfaces
{
    public interface ILiveTrackerRepository
    {
        Task OpenAsync(int streamId, string hostConnectionId);
        Task CloseAsync(int streamId);
        Task<string?> GetHostConnectionAsync(int streamId);
        Task<int> AddViewerAsync(int streamId, string connectionId, string userId = "");
        Task<int> RemoveViewerAsync(int streamId, string connectionId);
        Task<int> CountViewersAsync(int streamId);
        Task<IReadOnlyList<string>> GetViewerUserIdsAsync(int streamId);
        Task<string?> GetViewerUserIdAsync(int streamId, string connectionId);
        Task<bool> SetGuestAsync(int streamId, string connectionId);
        Task<string?> GetGuestAsync(int streamId);
        Task<string?> ClearGuestAsync(int streamId);
        Task<bool> IsParticipantAsync(int streamId, string connectionId);
        Task<IEnumerable<int>> CloseHostedAsync(string connectionId);
        Task<IEnumerable<int>> RemoveViewerEverywhereAsync(string connectionId);
    }
}
