using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerAudit;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerAuditService : IServerAuditService
    {
        private readonly IServerAuditLogRepository _logs;
        private readonly IServerPermissionService _permissions;
        private readonly IValidator<ServerAuditFilterDto> _filterValidator;

        public ServerAuditService(IServerAuditLogRepository logs,
                                  IServerPermissionService permissions,
                                  IValidator<ServerAuditFilterDto> filterValidator)
        {
            _logs = logs;
            _permissions = permissions;
            _filterValidator = filterValidator;
        }

        public async Task LogAsync(int serverId, string actorId, ServerAuditAction action, string targetType, string targetId, string targetName, string details)
        {
            await _logs.AddAsync(new ServerAuditLog
            {
                ServerId = serverId,
                ActorId = actorId,
                Action = action,
                TargetType = Cut(targetType, 32),
                TargetId = Cut(targetId, 450),
                TargetName = Cut(targetName, 200),
                Details = Cut(details, 1000)
            });
        }

        public async Task<IEnumerable<ServerAuditEntryDto>> GetAsync(string userId, int serverId, ServerAuditFilterDto filter)
        {
            await _filterValidator.EnsureValidAsync(filter);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ViewAuditLog);

            ServerAuditAction? action = null;
            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                action = Enum.Parse<ServerAuditAction>(filter.Action, true);
            }

            var entries = await _logs.GetForServerAsync(serverId, filter.ActorId, action, filter.BeforeId, filter.Take);
            return entries.Select(entry => new ServerAuditEntryDto
            {
                Id = entry.Id,
                Action = entry.Action.ToString(),
                ActorId = entry.ActorId,
                ActorUserName = entry.Actor.UserName ?? string.Empty,
                ActorDisplayName = string.IsNullOrWhiteSpace(entry.Actor.DisplayName) ? entry.Actor.UserName ?? string.Empty : entry.Actor.DisplayName,
                ActorAvatarUrl = entry.Actor.AvatarUrl,
                TargetType = entry.TargetType,
                TargetName = entry.TargetName,
                Details = entry.Details,
                CreatedAt = DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc)
            }).ToList();
        }

        private static string Cut(string value, int length)
        {
            value ??= string.Empty;
            return value.Length <= length ? value : value.Substring(0, length);
        }
    }
}
