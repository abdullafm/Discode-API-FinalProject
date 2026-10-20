using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Presence;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class PresenceTrackerService : IPresenceTrackerService
    {
        private readonly IPresenceTrackerRepository _trackerRepository;
        private readonly IPresenceService _presenceService;

        public PresenceTrackerService(IPresenceTrackerRepository trackerRepository,
                                      IPresenceService presenceService)
        {
            _trackerRepository = trackerRepository;
            _presenceService = presenceService;
        }

        public async Task<PresenceChangeDto> ConnectAsync(string userId, string connectionId)
        {
            var wasAway = await _trackerRepository.IsAwayAsync(userId);
            var first = await _trackerRepository.ConnectedAsync(userId, connectionId);

            try
            {
                return await _presenceService.GetChangeAsync(userId, first || wasAway);
            }
            catch
            {
                await _trackerRepository.DisconnectedAsync(userId, connectionId);
                throw;
            }
        }

        public async Task<PresenceChangeDto?> DisconnectAsync(string userId, string connectionId)
        {
            var wasAway = await _trackerRepository.IsAwayAsync(userId);
            var last = await _trackerRepository.DisconnectedAsync(userId, connectionId);
            if (!last && wasAway == await _trackerRepository.IsAwayAsync(userId))
            {
                return null;
            }

            return await _presenceService.GetChangeAsync(userId, true);
        }

        public async Task<PresenceChangeDto> SetAwayAsync(string userId, string connectionId, bool away)
        {
            await _trackerRepository.SetAwayAsync(userId, connectionId, away);
            return await _presenceService.GetChangeAsync(userId, true);
        }
    }
}
