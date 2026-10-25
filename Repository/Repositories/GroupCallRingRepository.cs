using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class GroupCallRingRepository : IGroupCallRingRepository
    {
        private readonly Dictionary<(int CallId, string UserId), DateTime?> _states = new();
        private readonly object _lock = new();

        public Task SetAsync(int callId, string userId, DateTime? ringingUntil)
        {
            lock (_lock)
            {
                _states[(callId, userId)] = ringingUntil;
                return Task.CompletedTask;
            }
        }

        public Task<Dictionary<string, DateTime?>> GetAsync(int callId)
        {
            lock (_lock)
            {
                var result = _states.Where(x => x.Key.CallId == callId).ToDictionary(x => x.Key.UserId, x => x.Value);
                return Task.FromResult(result);
            }
        }
    }
}
