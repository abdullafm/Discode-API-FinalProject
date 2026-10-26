using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerRoles;
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
        private readonly IServerMemberRepository _members;
        private readonly IServerPermissionService _permissions;
        private readonly IServerService _serverService;
        private readonly IValidator<CreateServerRoleDto> _createValidator;
        private readonly IValidator<UpdateServerRoleDto> _updateValidator;
        private readonly IMapper _mapper;

        public ServerRoleService(IServerRoleRepository roles,
                                 IServerMemberRoleRepository memberRoles,
                                 IServerMemberRepository members,
                                 IServerPermissionService permissions,
                                 IServerService serverService,
                                 IValidator<CreateServerRoleDto> createValidator,
                                 IValidator<UpdateServerRoleDto> updateValidator,
                                 IMapper mapper)
        {
            _roles = roles;
            _memberRoles = memberRoles;
            _members = members;
            _permissions = permissions;
            _serverService = serverService;
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
            return _mapper.Map<ServerRoleDto>(role);
        }

        public async Task<ServerRoleDto> UpdateAsync(string userId, int serverId, int roleId, UpdateServerRoleDto model)
        {
            await _updateValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            var role = await GetRoleAsync(serverId, roleId);
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
            await _roles.DeleteAsync(role);
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
            }
        }

        public async Task RemoveAsync(string userId, int serverId, int roleId, string targetUserId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageRoles);

            await GetRoleAsync(serverId, roleId);
            var target = await _members.GetAsync(serverId, targetUserId);
            if (target is null)
            {
                throw new NotFoundException("That person is not in this server.");
            }

            var link = await _memberRoles.GetAsync(roleId, target.Id);
            if (link is not null)
            {
                await _memberRoles.DeleteAsync(link);
            }
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
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
