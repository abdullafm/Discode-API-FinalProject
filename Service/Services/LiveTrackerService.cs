using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class LiveTrackerService : ILiveTrackerService
    {
        private readonly ILiveTrackerRepository _tracker;

        public LiveTrackerService(ILiveTrackerRepository tracker)
        {
            _tracker = tracker;
        }

        public Task OpenAsync(int streamId, string hostConnectionId)
        {
            return _tracker.OpenAsync(streamId, hostConnectionId);
        }

        public Task CloseAsync(int streamId)
        {
            return _tracker.CloseAsync(streamId);
        }

        public Task<string?> GetHostConnectionAsync(int streamId)
        {
            return _tracker.GetHostConnectionAsync(streamId);
        }

        public Task<int> AddViewerAsync(int streamId, string connectionId, string userId = "")
        {
            return _tracker.AddViewerAsync(streamId, connectionId, userId);
        }

        public Task<IReadOnlyList<string>> GetViewerUserIdsAsync(int streamId)
        {
            return _tracker.GetViewerUserIdsAsync(streamId);
        }

        public Task<int> RemoveViewerAsync(int streamId, string connectionId)
        {
            return _tracker.RemoveViewerAsync(streamId, connectionId);
        }

        public Task<int> CountViewersAsync(int streamId)
        {
            return _tracker.CountViewersAsync(streamId);
        }

        public Task<bool> IsParticipantAsync(int streamId, string connectionId)
        {
            return _tracker.IsParticipantAsync(streamId, connectionId);
        }

        public Task<IEnumerable<int>> CloseHostedAsync(string connectionId)
        {
            return _tracker.CloseHostedAsync(connectionId);
        }

        public Task<IEnumerable<int>> RemoveViewerEverywhereAsync(string connectionId)
        {
            return _tracker.RemoveViewerEverywhereAsync(connectionId);
        }
    }
}
