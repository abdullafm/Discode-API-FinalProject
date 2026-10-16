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
            var first = await _trackerRepository.ConnectedAsync(userId, connectionId);

            try
            {
                return await _presenceService.GetChangeAsync(userId, first);
            }
            catch
            {
                await _trackerRepository.DisconnectedAsync(userId, connectionId);
                throw;
            }
        }

        public async Task<PresenceChangeDto?> DisconnectAsync(string userId, string connectionId)
        {
            if (!await _trackerRepository.DisconnectedAsync(userId, connectionId))
            {
                return null;
            }

            return await _presenceService.GetChangeAsync(userId, true);
        }
    }
}
