using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerPrune;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerPruneService : IServerPruneService
    {
        private readonly IServerMemberRepository _members;
        private readonly IServerPermissionService _permissions;
        private readonly IServerAuditService _audit;
        private readonly IValidator<ServerPruneDto> _validator;

        public ServerPruneService(IServerMemberRepository members,
                                  IServerPermissionService permissions,
                                  IServerAuditService audit,
                                  IValidator<ServerPruneDto> validator)
        {
            _members = members;
            _permissions = permissions;
            _audit = audit;
            _validator = validator;
        }

        public async Task<int> PreviewAsync(string userId, int serverId, ServerPruneDto model)
        {
            await _validator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.KickMembers);

            var candidates = await _members.GetPrunableAsync(serverId, CutoffFor(model));
            return candidates.Count;
        }

        public async Task<ServerPruneChangeDto> PruneAsync(string userId, int serverId, ServerPruneDto model)
        {
            await _validator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.KickMembers);

            var candidates = await _members.GetPrunableAsync(serverId, CutoffFor(model));
            var removed = new List<string>();
            foreach (var member in candidates)
            {
                await _members.DeleteAsync(member);
                removed.Add(member.UserId);
            }

            if (removed.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberPrune, "Server", serverId.ToString(), string.Empty,
                    "Pruned " + removed.Count + " inactive " + (removed.Count == 1 ? "member" : "members") + " (no roles, no messages for " + model.Days + (model.Days == 1 ? " day)" : " days)"));
            }

            return new ServerPruneChangeDto
            {
                Count = removed.Count,
                RemovedUserIds = removed,
                RemainingUserIds = await _members.GetUserIdsAsync(serverId)
            };
        }

        private static DateTime CutoffFor(ServerPruneDto model)
        {
            return DateTime.UtcNow.AddDays(-model.Days);
        }
    }
}
