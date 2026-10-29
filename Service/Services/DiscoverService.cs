using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Discover;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class DiscoverService : IDiscoverService
    {
        private readonly IDiscoverRepository _discover;
        private readonly IPresenceService _presence;
        private readonly IValidator<DiscoverFilterDto> _filterValidator;

        public DiscoverService(IDiscoverRepository discover,
                               IPresenceService presence,
                               IValidator<DiscoverFilterDto> filterValidator)
        {
            _discover = discover;
            _presence = presence;
            _filterValidator = filterValidator;
        }

        public async Task<IEnumerable<DiscoverServerDto>> GetAsync(DiscoverFilterDto filter)
        {
            await _filterValidator.EnsureValidAsync(filter);

            var servers = await _discover.GetPublicAsync(filter.Search, filter.Take);
            var result = new List<DiscoverServerDto>();
            foreach (var server in servers)
            {
                result.Add(await ToDtoAsync(server));
            }

            return result;
        }

        public async Task<DiscoverServerDto?> GetItemAsync(int serverId)
        {
            var server = await _discover.GetPublicAsync(serverId);
            return server is null ? null : await ToDtoAsync(server);
        }

        public async Task<IEnumerable<DiscoverServerDto>> GetForUserAsync(string userId)
        {
            var result = new List<DiscoverServerDto>();
            foreach (var server in await _discover.GetPublicForUserAsync(userId))
            {
                result.Add(await ToDtoAsync(server));
            }

            return result;
        }

        private async Task<DiscoverServerDto> ToDtoAsync(Server server)
        {
            var online = 0;
            foreach (var member in server.Members)
            {
                if (await _presence.ResolveAsync(member.User) != "offline")
                {
                    online++;
                }
            }

            return new DiscoverServerDto
            {
                Id = server.Id,
                Name = server.Name,
                IconUrl = server.IconUrl,
                BannerUrl = server.BannerUrl,
                BannerColor = server.BannerColor,
                Description = server.Description,
                InviteCode = server.InviteCode,
                MemberCount = server.Members.Count,
                OnlineCount = online
            };
        }
    }
}
