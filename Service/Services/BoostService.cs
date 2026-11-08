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
        private readonly IBoostCooldownRepository _cooldowns;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly INitroAccessService _access;

        public BoostService(IServerBoostRepository boosts, IBoostCooldownRepository cooldowns, IServerRepository servers, IServerMemberRepository members, INitroAccessService access)
        {
            _boosts = boosts;
            _cooldowns = cooldowns;
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

            var used = await _boosts.CountForUserAsync(userId);
            var waiting = await _cooldowns.GetActiveAsync(userId, DateTime.UtcNow);
            if (used + waiting.Count >= NitroRules.BoostsPerNitro)
            {
                if (used >= NitroRules.BoostsPerNitro)
                {
                    throw new BadRequestException("You have already used both of your boosts.");
                }

                throw new BadRequestException("Your boost can be used again after " + waiting[0].AvailableAt.ToString("yyyy-MM-dd HH:mm") + " UTC.");
            }

            await _boosts.AddAsync(new ServerBoost { ServerId = serverId, UserId = userId });
            return await ChangeAsync(userId, serverId);
        }

        // taking a boost off is allowed at any time, but its place is only free again after the cooldown (so boosts can not hop around at once)
        public async Task<BoostChangeDto> UnboostAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);
            var mine = await _boosts.GetForUserOnServerAsync(userId, serverId);
            if (mine.Count == 0)
            {
                throw new BadRequestException("You have not boosted this server.");
            }

            await _boosts.DeleteAsync(mine[0]);
            await _cooldowns.DeleteForUserAsync(userId, DateTime.UtcNow);
            await _cooldowns.AddAsync(new BoostCooldown { UserId = userId, AvailableAt = DateTime.UtcNow.AddHours(NitroRules.BoostCooldownHours) });
            await EnforcePerksAsync(serverId);
            return await ChangeAsync(userId, serverId);
        }

        // one boost goes to another server (it has to have stayed where it is for the cooldown time); the old server may lose its perks
        public async Task<BoostChangeDto> MoveAsync(string userId, MoveBoostDto model)
        {
            var from = model?.FromServerId ?? 0;
            var to = model?.ToServerId ?? 0;
            if (from <= 0 || to <= 0 || from == to)
            {
                throw new BadRequestException("Choose another server to move the boost to.");
            }

            await EnsureMemberAsync(userId, to);
            if (!await _access.HasNitroAsync(userId))
            {
                throw new ForbiddenException("Boosting a server needs Nitro.");
            }

            var mine = (await _boosts.GetForUserOnServerAsync(userId, from)).OrderBy(x => x.CreatedAt).ToList();
            if (mine.Count == 0)
            {
                throw new BadRequestException("You have not boosted that server.");
            }

            var ready = DateTime.UtcNow.AddHours(-NitroRules.BoostCooldownHours);
            var movable = mine.FirstOrDefault(x => x.CreatedAt <= ready);
            if (movable is null)
            {
                throw new BadRequestException("This boost can be moved after " + mine[0].CreatedAt.AddHours(NitroRules.BoostCooldownHours).ToString("yyyy-MM-dd HH:mm") + " UTC.");
            }

            movable.ServerId = to;
            movable.CreatedAt = DateTime.UtcNow;
            await _boosts.UpdateAsync(movable);
            await EnforcePerksAsync(from);

            var result = await ChangeAsync(userId, to);
            result.Change.ServerIds.Add(from);
            return result;
        }

        public async Task<MyBoostsDto> GetMineAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var has = await _access.HasNitroAsync(userId);
            var mine = (await _boosts.GetForUserAsync(userId)).OrderBy(x => x.CreatedAt).ToList();
            var waiting = await _cooldowns.GetActiveAsync(userId, now);
            var servers = (await _servers.GetForUserAsync(userId)).ToList();
            var counts = await _boosts.CountForServersAsync(servers.Select(s => s.Id));
            var names = servers.ToDictionary(s => s.Id, s => s.Name);

            var result = new MyBoostsDto
            {
                HasNitro = has,
                Slots = has ? NitroRules.BoostsPerNitro : 0,
                Used = mine.Count,
                Required = NitroRules.BoostsForPerks,
                CooldownHours = NitroRules.BoostCooldownHours,
                Cooldowns = waiting.Select(x => DateTime.SpecifyKind(x.AvailableAt, DateTimeKind.Utc)).ToList()
            };
            result.Free = has ? Math.Max(0, result.Slots - mine.Count - waiting.Count) : 0;

            foreach (var boost in mine)
            {
                var movableAt = boost.CreatedAt.AddHours(NitroRules.BoostCooldownHours);
                result.Boosts.Add(new MyBoostDto
                {
                    ServerId = boost.ServerId,
                    ServerName = names.TryGetValue(boost.ServerId, out var name) ? name : "A server you left",
                    BoostedAt = DateTime.SpecifyKind(boost.CreatedAt, DateTimeKind.Utc),
                    MovableAt = DateTime.SpecifyKind(movableAt, DateTimeKind.Utc),
                    CanMove = movableAt <= now
                });
            }

            foreach (var server in servers.OrderByDescending(s => counts.TryGetValue(s.Id, out var c) ? c : 0).ThenBy(s => s.Name))
            {
                var total = counts.TryGetValue(server.Id, out var count) ? count : 0;
                result.Servers.Add(new BoostableServerDto
                {
                    ServerId = server.Id,
                    Name = server.Name,
                    IconUrl = server.IconUrl,
                    Boosts = total,
                    PerksUnlocked = total >= NitroRules.BoostsForPerks,
                    MyBoosts = mine.Count(x => x.ServerId == server.Id)
                });
            }

            return result;
        }

        public async Task<List<int>> RemoveAllForUserAsync(string userId)
        {
            await _cooldowns.DeleteForUserAsync(userId);
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

        public Task<HashSet<string>> GetBoostersAmongAsync(IEnumerable<string> userIds)
        {
            return _boosts.GetBoosterIdsAmongAsync(userIds);
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
            var waiting = await _cooldowns.GetActiveAsync(userId, DateTime.UtcNow);
            var has = await _access.HasNitroAsync(userId);
            var left = has ? Math.Max(0, NitroRules.BoostsPerNitro - used - waiting.Count) : 0;
            return new BoostInfoDto
            {
                ServerId = serverId,
                Boosts = all.Count,
                Required = NitroRules.BoostsForPerks,
                PerksUnlocked = all.Count >= NitroRules.BoostsForPerks,
                MyBoosts = all.Count(x => x.UserId == userId),
                MyBoostsLeft = left,
                CanBoost = left > 0,
                CooldownUntil = has && waiting.Count > 0 ? DateTime.SpecifyKind(waiting[0].AvailableAt, DateTimeKind.Utc) : null,
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
