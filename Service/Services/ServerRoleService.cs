using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerRoles;
using System.Text.RegularExpressions;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerRoleService : IServerRoleService
    {
        private readonly IServerRoleRepository _roles;
        private readonly IServerMemberRoleRepository _memberRoles;
        private readonly IServerChannelAccessRepository _access;
        private readonly IServerMemberRepository _members;
        private readonly IServerPermissionService _permissions;
        private readonly IServerService _serverService;
        private readonly IServerAuditService _audit;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateServerRoleDto> _createValidator;
        private readonly IValidator<UpdateServerRoleDto> _updateValidator;
        private readonly IMapper _mapper;

        public ServerRoleService(IServerRoleRepository roles,
                                 IServerMemberRoleRepository memberRoles,
                                 IServerChannelAccessRepository access,
                                 IServerMemberRepository members,
                                 IServerPermissionService permissions,
                                 IServerService serverService,
                                 IServerAuditService audit,
                                 IAccountRepository accounts,
                                 IValidator<CreateServerRoleDto> createValidator,
                                 IValidator<UpdateServerRoleDto> updateValidator,
                                 IMapper mapper)
        {
            _roles = roles;
            _memberRoles = memberRoles;
            _access = access;
            _members = members;
            _permissions = permissions;
            _serverService = serverService;
            _audit = audit;
            _accounts = accounts;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ServerRoleDto>> GetRolesAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);
            await _permissions.EnsureEveryoneAsync(serverId);

            var memberCount = (await _members.GetUserIdsAsync(serverId)).Count;
            var roles = (await _roles.GetForServerAsync(serverId))
                .OrderByDescending(r => !r.IsEveryone)
                .ThenByDescending(r => r.Position)
                .ToList();
            var dtos = _mapper.Map<List<ServerRoleDto>>(roles);
            foreach (var dto in dtos.Where(d => d.IsEveryone))
            {
                dto.MemberCount = memberCount;
            }

            return dtos;
        }

        public async Task<ServerRoleDto> CreateAsync(string userId, int serverId, CreateServerRoleDto model)
        {
            await _createValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var name = model.Name.Trim();
            if (await _roles.NameExistsAsync(serverId, name, null))
            {
                throw new BadRequestException("A role with that name already exists.");
            }

            var role = new ServerRole
            {
                ServerId = serverId,
                Name = name,
                Color = "gray",
                Permissions = 0,
                Position = await _roles.NextPositionAsync(serverId)
            };
            await _roles.AddAsync(role);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.RoleCreate, "Role", role.Id.ToString(), role.Name, "Created the role " + role.Name);
            return _mapper.Map<ServerRoleDto>(role);
        }

        public async Task<ServerRoleDto> UpdateAsync(string userId, int serverId, int roleId, UpdateServerRoleDto model)
        {
            await _updateValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var role = await GetRoleAsync(serverId, roleId);
            var oldName = role.Name;
            var oldColor = role.Color;
            var oldBits = role.Permissions;
            var oldSeparate = role.DisplaySeparately;
            if (!role.IsEveryone)
            {
                var name = model.Name.Trim();
                if (await _roles.NameExistsAsync(serverId, name, roleId))
                {
                    throw new BadRequestException("A role with that name already exists.");
                }

                role.Name = name;
            }

            role.Color = model.Color.Trim().ToLowerInvariant();
            role.Permissions = model.Permissions & (long)ServerPermission.All;
            role.DisplaySeparately = model.DisplaySeparately;
            await _roles.UpdateAsync(role);

            var changes = new List<string>();
            if (oldName != role.Name)
            {
                changes.Add("Renamed the role from \"" + oldName + "\" to \"" + role.Name + "\"");
            }

            if (oldColor != role.Color)
            {
                changes.Add("Changed the color from " + oldColor + " to " + role.Color);
            }

            var added = role.Permissions & ~oldBits;
            var removed = oldBits & ~role.Permissions;
            if (added != 0)
            {
                changes.Add("Allowed " + DescribePermissions(added));
            }

            if (removed != 0)
            {
                changes.Add("Removed " + DescribePermissions(removed));
            }

            if (oldSeparate != role.DisplaySeparately)
            {
                changes.Add(role.DisplaySeparately ? "Turned on showing role members separately" : "Turned off showing role members separately");
            }

            if (changes.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.RoleUpdate, "Role", role.Id.ToString(), role.Name, string.Join("; ", changes));
            }

            return _mapper.Map<ServerRoleDto>(role);
        }

        public async Task DeleteAsync(string userId, int serverId, int roleId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var role = await GetRoleAsync(serverId, roleId);
            if (role.IsEveryone)
            {
                throw new BadRequestException("The @everyone role can't be deleted.");
            }

            await _memberRoles.DeleteForRoleAsync(roleId);
            await _access.DeleteForRoleAsync(roleId);
            await _roles.DeleteAsync(role);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.RoleDelete, "Role", roleId.ToString(), role.Name, "Deleted the role " + role.Name);
        }

        public async Task<IEnumerable<ServerMemberDto>> GetRoleMembersAsync(string userId, int serverId, int roleId)
        {
            await EnsureMemberAsync(userId, serverId);
            var role = await GetRoleAsync(serverId, roleId);
            var members = await _serverService.GetMembersAsync(userId, serverId);
            return role.IsEveryone ? members : members.Where(m => m.Roles.Any(r => r.Id == roleId)).ToList();
        }

        public async Task AssignAsync(string userId, int serverId, int roleId, string targetUserId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var role = await GetRoleAsync(serverId, roleId);
            if (role.IsEveryone)
            {
                throw new BadRequestException("Everyone already has the @everyone role.");
            }

            var target = await _members.GetAsync(serverId, targetUserId);
            if (target is null)
            {
                throw new NotFoundException("That person is not in this server.");
            }

            if (await _memberRoles.GetAsync(roleId, target.Id) is null)
            {
                await _memberRoles.AddAsync(new ServerMemberRole { RoleId = roleId, ServerMemberId = target.Id });
                var person = await _accounts.GetByIdAsync(targetUserId);
                await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberRoleUpdate, "Member", targetUserId, person?.DisplayName ?? string.Empty, "Added the role " + role.Name);
            }
        }

        public async Task RemoveAsync(string userId, int serverId, int roleId, string targetUserId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var removedRole = await GetRoleAsync(serverId, roleId);
            var target = await _members.GetAsync(serverId, targetUserId);
            if (target is null)
            {
                throw new NotFoundException("That person is not in this server.");
            }

            var link = await _memberRoles.GetAsync(roleId, target.Id);
            if (link is not null)
            {
                await _memberRoles.DeleteAsync(link);
                var person = await _accounts.GetByIdAsync(targetUserId);
                await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberRoleUpdate, "Member", targetUserId, person?.DisplayName ?? string.Empty, "Removed the role " + removedRole.Name);
            }
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
        }

        private static string DescribePermissions(long bits)
        {
            var names = new List<string>();
            foreach (ServerPermission permission in Enum.GetValues(typeof(ServerPermission)))
            {
                if (permission == ServerPermission.None || permission == ServerPermission.All || (bits & (long)permission) == 0)
                {
                    continue;
                }

                names.Add(Regex.Replace(permission.ToString(), "(?<!^)([A-Z])", " $1"));
            }

            return string.Join(", ", names);
        }

        private async Task EnsureMemberAsync(string userId, int serverId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }
        }

        private async Task<ServerRole> GetRoleAsync(int serverId, int roleId)
        {
            var role = await _roles.GetInServerAsync(serverId, roleId);
            if (role is null)
            {
                throw new NotFoundException("Role not found.");
            }

            return role;
        }
    }
}
