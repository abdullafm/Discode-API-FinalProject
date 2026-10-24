using Domain.Entities;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class LiveTrackerRepository : ILiveTrackerRepository
    {
        private readonly Dictionary<int, LiveRoom> _rooms = new();
        private readonly object _lock = new();

        public Task OpenAsync(int streamId, string hostConnectionId)
        {
            lock (_lock)
            {
                _rooms[streamId] = new LiveRoom { HostConnectionId = hostConnectionId };
                return Task.CompletedTask;
            }
        }

        public Task CloseAsync(int streamId)
        {
            lock (_lock)
            {
                _rooms.Remove(streamId);
                return Task.CompletedTask;
            }
        }

        public Task<string?> GetHostConnectionAsync(int streamId)
        {
            lock (_lock)
            {
                return Task.FromResult(_rooms.TryGetValue(streamId, out var room) ? room.HostConnectionId : null);
            }
        }

        public Task<int> AddViewerAsync(int streamId, string connectionId, string userId = "")
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(streamId, out var room))
                {
                    return Task.FromResult(0);
                }

                room.Viewers.Add(connectionId);
                room.ViewerUsers[connectionId] = userId;
                return Task.FromResult(room.Viewers.Count);
            }
        }

        public Task<int> RemoveViewerAsync(int streamId, string connectionId)
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(streamId, out var room))
                {
                    return Task.FromResult(0);
                }

                room.Viewers.Remove(connectionId);
                room.ViewerUsers.Remove(connectionId);
                return Task.FromResult(room.Viewers.Count);
            }
        }

        public Task<int> CountViewersAsync(int streamId)
        {
            lock (_lock)
            {
                return Task.FromResult(_rooms.TryGetValue(streamId, out var room) ? room.Viewers.Count : 0);
            }
        }

        public Task<IReadOnlyList<string>> GetViewerUserIdsAsync(int streamId)
        {
            lock (_lock)
            {
                IReadOnlyList<string> ids = _rooms.TryGetValue(streamId, out var room)
                    ? room.ViewerUsers.Values.Where(x => x.Length > 0).Distinct().ToList()
                    : new List<string>();
                return Task.FromResult(ids);
            }
        }

        public Task<bool> IsParticipantAsync(int streamId, string connectionId)
        {
            lock (_lock)
            {
                return Task.FromResult(_rooms.TryGetValue(streamId, out var room) &&
                                       (room.HostConnectionId == connectionId || room.Viewers.Contains(connectionId)));
            }
        }

        public Task<IEnumerable<int>> CloseHostedAsync(string connectionId)
        {
            lock (_lock)
            {
                var hosted = _rooms.Where(x => x.Value.HostConnectionId == connectionId).Select(x => x.Key).ToList();
                foreach (var id in hosted)
                {
                    _rooms.Remove(id);
                }

                return Task.FromResult<IEnumerable<int>>(hosted);
            }
        }

        public Task<IEnumerable<int>> RemoveViewerEverywhereAsync(string connectionId)
        {
            lock (_lock)
            {
                var viewed = _rooms.Where(x => x.Value.Viewers.Remove(connectionId)).Select(x => x.Key).ToList();
                foreach (var room in _rooms.Values)
                {
                    room.ViewerUsers.Remove(connectionId);
                }

                return Task.FromResult<IEnumerable<int>>(viewed);
            }
        }
    }
}
