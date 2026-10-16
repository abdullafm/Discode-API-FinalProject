using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Presence;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class PresenceService : IPresenceService
    {
        private const string Offline = "offline";

        private readonly IPresenceRepository _presenceRepository;
        private readonly IPresenceTrackerRepository _trackerRepository;
        private readonly IMapper _mapper;

        public PresenceService(IPresenceRepository presenceRepository,
                               IPresenceTrackerRepository trackerRepository,
                               IMapper mapper)
        {
            _presenceRepository = presenceRepository;
            _trackerRepository = trackerRepository;
            _mapper = mapper;
        }

        public async Task<PresenceChangeDto> GetChangeAsync(string userId, bool shouldBroadcast)
        {
            var user = await GetUserAsync(userId);
            return await MapChangeAsync(user, shouldBroadcast);
        }

        public async Task<PresenceChangeDto> SetStatusAsync(string userId, string status)
        {
            var preferred = PresenceStatusMap.Parse(status);
            var user = await GetUserAsync(userId);

            if (!await _presenceRepository.SetPreferredStatusAsync(userId, preferred))
            {
                throw new NotFoundException("User not found.");
            }

            user.PreferredStatus = preferred;
            return await MapChangeAsync(user, true);
        }

        public async Task<string> ResolveAsync(AppUser user)
        {
            if (user.PreferredStatus == PresenceStatus.Invisible || !await _trackerRepository.IsOnlineAsync(user.Id))
            {
                return Offline;
            }

            return PresenceStatusMap.ToWire(user.PreferredStatus);
        }

        private async Task<AppUser> GetUserAsync(string userId)
        {
            var user = await _presenceRepository.GetUserAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            return user;
        }

        private async Task<PresenceChangeDto> MapChangeAsync(AppUser user, bool shouldBroadcast)
        {
            var change = _mapper.Map<PresenceChangeDto>(user);
            change.Status = await ResolveAsync(user);
            change.ShouldBroadcast = shouldBroadcast;
            change.FriendIds = await _presenceRepository.GetFriendIdsAsync(user.Id);
            return change;
        }
    }
}
