using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerTimeoutService : IServerTimeoutService
    {
        private readonly IServerMemberRepository _members;
        private readonly IAccountRepository _accounts;
        private readonly IServerPermissionService _permissions;
        private readonly IServerAuditService _audit;
        private readonly IValidator<TimeoutServerMemberDto> _validator;

        public ServerTimeoutService(IServerMemberRepository members,
                                    IAccountRepository accounts,
                                    IServerPermissionService permissions,
                                    IServerAuditService audit,
                                    IValidator<TimeoutServerMemberDto> validator)
        {
            _members = members;
            _accounts = accounts;
            _permissions = permissions;
            _audit = audit;
            _validator = validator;
        }

        public async Task SetAsync(string userId, int serverId, string targetUserId, TimeoutServerMemberDto model)
        {
            await _validator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ModerateMembers);

            if (targetUserId == userId)
            {
                throw new BadRequestException("You can't time yourself out.");
            }

            var membership = await _members.GetAsync(serverId, targetUserId);
            if (membership is null)
            {
                throw new NotFoundException("That person is not in this server.");
            }

            await _permissions.EnsureCanActOnAsync(userId, serverId, targetUserId, "time out");

            var person = await _accounts.GetByIdAsync(targetUserId);
            var name = person is null ? string.Empty : string.IsNullOrWhiteSpace(person.DisplayName) ? person.UserName ?? string.Empty : person.DisplayName;
            if (model.Minutes == 0)
            {
                membership.TimeoutUntil = null;
                await _members.UpdateAsync(membership);
                await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberTimeoutRemove, "Member", targetUserId, name, "Removed the timeout");
                return;
            }

            membership.TimeoutUntil = DateTime.UtcNow.AddMinutes(model.Minutes);
            await _members.UpdateAsync(membership);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberTimeout, "Member", targetUserId, name, "Timed out for " + Describe(model.Minutes));
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
        }

        private static string Describe(int minutes)
        {
            if (minutes % (24 * 60) == 0)
            {
                var days = minutes / (24 * 60);
                return days + (days == 1 ? " day" : " days");
            }

            if (minutes % 60 == 0)
            {
                var hours = minutes / 60;
                return hours + (hours == 1 ? " hour" : " hours");
            }

            return minutes + (minutes == 1 ? " minute" : " minutes");
        }
    }
}
