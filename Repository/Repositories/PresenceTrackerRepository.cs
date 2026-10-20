using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class PresenceTrackerRepository : IPresenceTrackerRepository
    {
        private readonly Dictionary<string, HashSet<string>> _connections = new();
        private readonly HashSet<string> _away = new();
        private readonly object _lock = new();

        public Task<bool> ConnectedAsync(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var ids))
                {
                    ids = new HashSet<string>();
                    _connections[userId] = ids;
                }

                ids.Add(connectionId);
                return Task.FromResult(ids.Count == 1);
            }
        }

        public Task<bool> DisconnectedAsync(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var ids))
                {
                    return Task.FromResult(false);
                }

                ids.Remove(connectionId);
                _away.Remove(connectionId);
                if (ids.Count > 0)
                {
                    return Task.FromResult(false);
                }

                _connections.Remove(userId);
                return Task.FromResult(true);
            }
        }

        public Task SetAwayAsync(string userId, string connectionId, bool away)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var ids) || !ids.Contains(connectionId))
                {
                    return Task.CompletedTask;
                }

                if (away)
                {
                    _away.Add(connectionId);
                }
                else
                {
                    _away.Remove(connectionId);
                }

                return Task.CompletedTask;
            }
        }

        public Task<bool> IsAwayAsync(string userId)
        {
            lock (_lock)
            {
                return Task.FromResult(_connections.TryGetValue(userId, out var ids) && ids.Count > 0 && ids.All(_away.Contains));
            }
        }

        public Task<bool> IsOnlineAsync(string userId)
        {
            lock (_lock)
            {
                return Task.FromResult(_connections.ContainsKey(userId));
            }
        }
    }
}
