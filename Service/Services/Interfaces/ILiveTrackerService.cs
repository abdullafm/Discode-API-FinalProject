namespace Service.Services.Interfaces
{
    public interface ILiveTrackerService
    {
        Task OpenAsync(int streamId, string hostConnectionId);
        Task CloseAsync(int streamId);
        Task<string?> GetHostConnectionAsync(int streamId);
        Task<int> AddViewerAsync(int streamId, string connectionId);
        Task<int> RemoveViewerAsync(int streamId, string connectionId);
        Task<int> CountViewersAsync(int streamId);
        Task<bool> IsParticipantAsync(int streamId, string connectionId);
        Task<IEnumerable<int>> CloseHostedAsync(string connectionId);
        Task<IEnumerable<int>> RemoveViewerEverywhereAsync(string connectionId);
    }
}
