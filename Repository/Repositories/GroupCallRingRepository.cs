using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class GroupCallRingRepository : IGroupCallRingRepository
    {
        private readonly Dictionary<(int CallId, string UserId), DateTime> _rings = new();
        private readonly object _lock = new();

        public Task RingAsync(int callId, string userId, TimeSpan window)
        {
            lock (_lock)
            {
                _rings[(callId, userId)] = DateTime.UtcNow.Add(window);
                return Task.CompletedTask;
            }
        }

        public Task<bool> StopAsync(int callId, string userId)
        {
            lock (_lock)
            {
                return Task.FromResult(_rings.Remove((callId, userId)));
            }
        }

        public Task<IEnumerable<string>> GetRingingAsync(int callId)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var expired = _rings.Where(x => x.Value <= now).Select(x => x.Key).ToList();
                foreach (var key in expired)
                {
                    _rings.Remove(key);
                }

                IEnumerable<string> ringing = _rings.Keys.Where(x => x.CallId == callId).Select(x => x.UserId).ToList();
                return Task.FromResult(ringing);
            }
        }
    }
}
