using Domain.Common;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Nitro;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BoostService : IBoostService
    {
        private readonly IServerBoostRepository _boosts;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly INitroAccessService _access;

        public BoostService(IServerBoostRepository boosts, IServerRepository servers, IServerMemberRepository members, INitroAccessService access)
        {
            _boosts = boosts;
            _servers = servers;
            _members = members;
            _access = access;
        }

        public async Task<BoostInfoDto> GetInfoAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);
            return await BuildAsync(userId, serverId);
        }

        public async Task<BoostChangeDto> BoostAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);
            if (!await _access.HasNitroAsync(userId))
            {
                throw new ForbiddenException("Boosting a server needs Nitro.");
            }

            if (await _boosts.CountForUserAsync(userId) >= NitroRules.BoostsPerNitro)
            {
                throw new BadRequestException("You have already used both of your boosts.");
            }

            await _boosts.AddAsync(new ServerBoost { ServerId = serverId, UserId = userId });
            return await ChangeAsync(userId, serverId);
        }

        public async Task<BoostChangeDto> UnboostAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);
            var mine = await _boosts.GetForUserOnServerAsync(userId, serverId);
            if (mine.Count == 0)
            {
                throw new BadRequestException("You have not boosted this server.");
            }

            await _boosts.DeleteAsync(mine[0]);
            await EnforcePerksAsync(serverId);
            return await ChangeAsync(userId, serverId);
        }

        public async Task<List<int>> RemoveAllForUserAsync(string userId)
        {
            var all = await _boosts.GetForUserAsync(userId);
            if (all.Count == 0)
            {
                return new List<int>();
            }

            var serverIds = all.Select(x => x.ServerId).Distinct().ToList();
            await _boosts.DeleteRangeAsync(all);
            foreach (var serverId in serverIds)
            {
                await EnforcePerksAsync(serverId);
            }

            return serverIds;
        }

        public async Task<bool> PerksUnlockedAsync(int serverId)
        {
            return await _boosts.CountForServerAsync(serverId) >= NitroRules.BoostsForPerks;
        }

        public Task<Dictionary<int, int>> GetCountsAsync(IEnumerable<int> serverIds)
        {
            return _boosts.CountForServersAsync(serverIds);
        }

        public async Task<HashSet<string>> GetBoosterIdsAsync(int serverId)
        {
            return (await _boosts.GetBoosterIdsAsync(serverId)).ToHashSet();
        }

        public async Task<bool> IsBoosterAsync(string userId)
        {
            return await _boosts.CountForUserAsync(userId) > 0;
        }

        public Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return _members.GetUserIdsAsync(serverId);
        }

        // below 14 boosts the server loses what the boosts unlocked: the animated icon, the banner and the short link
        // (the random invite code keeps working)
        private async Task EnforcePerksAsync(int serverId)
        {
            if (await PerksUnlockedAsync(serverId))
            {
                return;
            }

            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                return;
            }

            var changed = false;
            if (NitroRules.IsAnimatedImage(server.IconUrl))
            {
                server.IconUrl = string.Empty;
                changed = true;
            }

            if (!string.IsNullOrEmpty(server.BannerUrl))
            {
                server.BannerUrl = string.Empty;
                changed = true;
            }

            if (server.CustomLink is not null)
            {
                server.CustomLink = null;
                changed = true;
            }

            if (changed)
            {
                await _servers.UpdateAsync(server);
            }
        }

        private async Task EnsureMemberAsync(string userId, int serverId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }
        }

        private async Task<BoostChangeDto> ChangeAsync(string userId, int serverId)
        {
            var result = new BoostChangeDto { Info = await BuildAsync(userId, serverId) };
            result.Change.ServerIds.Add(serverId);
            result.Change.UserIds.Add(userId);
            return result;
        }

        private async Task<BoostInfoDto> BuildAsync(string userId, int serverId)
        {
            var all = await _boosts.GetForServerAsync(serverId);
            var used = await _boosts.CountForUserAsync(userId);
            var has = await _access.HasNitroAsync(userId);
            var left = has ? Math.Max(0, NitroRules.BoostsPerNitro - used) : 0;
            return new BoostInfoDto
            {
                ServerId = serverId,
                Boosts = all.Count,
                Required = NitroRules.BoostsForPerks,
                PerksUnlocked = all.Count >= NitroRules.BoostsForPerks,
                MyBoosts = all.Count(x => x.UserId == userId),
                MyBoostsLeft = left,
                CanBoost = left > 0,
                Boosters = all.GroupBy(x => x.UserId).Select(group => new BoosterDto
                {
                    UserName = group.First().User.UserName ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(group.First().User.DisplayName) ? group.First().User.UserName ?? string.Empty : group.First().User.DisplayName,
                    AvatarUrl = group.First().User.AvatarUrl,
                    Count = group.Count()
                }).ToList()
            };
        }
    }
}
