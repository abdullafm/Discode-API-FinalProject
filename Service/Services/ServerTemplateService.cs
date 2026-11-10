using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerChannels;
using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.DTOs.ServerTemplates;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace Service.Services
{
    public class ServerTemplateService : IServerTemplateService
    {
        private const string CodeAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int MaxChannels = 100;
        private const int MaxRoles = 50;
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly IServerTemplateRepository _templates;
        private readonly IServerRepository _servers;
        private readonly IServerService _serverService;
        private readonly IServerChannelService _channels;
        private readonly IServerRoleService _roles;
        private readonly IServerPermissionService _permissions;
        private readonly IAccountRepository _accounts;

        public ServerTemplateService(IServerTemplateRepository templates, IServerRepository servers, IServerService serverService, IServerChannelService channels,
                                     IServerRoleService roles, IServerPermissionService permissions, IAccountRepository accounts)
        {
            _templates = templates;
            _servers = servers;
            _serverService = serverService;
            _channels = channels;
            _roles = roles;
            _permissions = permissions;
            _accounts = accounts;
        }

        public async Task<ServerTemplateDto?> GetAsync(string userId, int serverId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);
            var template = await _templates.GetForServerAsync(serverId);
            if (template is null)
            {
                return null;
            }

            return await ToDtoAsync(userId, template, true);
        }

        public async Task<ServerTemplateDto> SaveAsync(string userId, int serverId, SaveServerTemplateDto model)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);
            var title = (model?.Title ?? string.Empty).Trim();
            var description = (model?.Description ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                throw new BadRequestException("Give the template a title.");
            }

            if (title.Length > 100)
            {
                throw new BadRequestException("The title can be at most 100 characters.");
            }

            if (description.Length > 120)
            {
                throw new BadRequestException("The description can be at most 120 characters.");
            }

            var template = await _templates.GetForServerAsync(serverId);
            if (template is null)
            {
                template = new ServerTemplate
                {
                    ServerId = serverId,
                    Code = await NewCodeAsync(),
                    Title = title,
                    Description = description,
                    CreatedById = userId,
                    Snapshot = JsonSerializer.Serialize(await TakeSnapshotAsync(userId, serverId), Json),
                    SyncedAt = DateTime.UtcNow
                };
                await _templates.AddAsync(template);
                template = (await _templates.GetForServerAsync(serverId))!;
            }
            else
            {
                template.Title = title;
                template.Description = description;
                await _templates.UpdateAsync(template);
            }

            return await ToDtoAsync(userId, template, true);
        }

        public async Task<ServerTemplateDto> SyncAsync(string userId, int serverId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);
            var template = await _templates.GetForServerAsync(serverId) ?? throw new NotFoundException("This server has no template yet.");
            template.Snapshot = JsonSerializer.Serialize(await TakeSnapshotAsync(userId, serverId), Json);
            template.SyncedAt = DateTime.UtcNow;
            await _templates.UpdateAsync(template);
            return await ToDtoAsync(userId, template, true);
        }

        public async Task DeleteAsync(string userId, int serverId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);
            var template = await _templates.GetForServerAsync(serverId) ?? throw new NotFoundException("This server has no template.");
            await _templates.DeleteAsync(template);
        }

        public async Task<ServerTemplateInfoDto> GetInfoAsync(string code)
        {
            var template = await _templates.GetByCodeAsync((code ?? string.Empty).Trim()) ?? throw new NotFoundException("That template does not exist any more.");
            var snapshot = Read(template);
            var creator = await _accounts.GetByIdAsync(template.CreatedById);
            var info = new ServerTemplateInfoDto
            {
                Code = template.Code,
                Title = template.Title,
                Description = template.Description,
                UsageCount = template.UsageCount,
                RoleCount = snapshot.Roles.Count,
                CategoryCount = snapshot.Categories.Count,
                ChannelCount = Count(snapshot),
                CreatedAt = template.CreatedAt,
                SyncedAt = template.SyncedAt,
                ServerName = template.Server?.Name ?? string.Empty,
                CreatorName = creator?.DisplayName ?? string.Empty,
                Categories = snapshot.Categories,
                Loose = snapshot.Loose,
                Roles = snapshot.Roles.Select(r => r.Name).ToList()
            };
            return info;
        }

        public async Task<ServerChangeDto> UseAsync(string userId, string code, UseServerTemplateDto model)
        {
            var template = await _templates.GetByCodeAsync((code ?? string.Empty).Trim()) ?? throw new NotFoundException("That template does not exist any more.");
            var snapshot = Read(template);
            var created = await _serverService.CreateBlankAsync(userId, new CreateServerDto { Name = model?.Name ?? string.Empty });
            var serverId = created.Server.Id;

            foreach (var role in snapshot.Roles.Take(MaxRoles))
            {
                var made = await _roles.CreateAsync(userId, serverId, new CreateServerRoleDto { Name = role.Name });
                await _roles.UpdateAsync(userId, serverId, made.Id, new UpdateServerRoleDto
                {
                    Name = role.Name,
                    Color = role.Color,
                    Permissions = role.Permissions,
                    DisplaySeparately = role.DisplaySeparately
                });
            }

            foreach (var channel in snapshot.Loose)
            {
                await MakeChannelAsync(userId, serverId, null, channel);
            }

            foreach (var category in snapshot.Categories)
            {
                var made = await _channels.CreateCategoryAsync(userId, serverId, new CreateServerCategoryDto { Name = category.Name, IsPrivate = category.IsPrivate });
                foreach (var channel in category.Channels)
                {
                    await MakeChannelAsync(userId, serverId, made.Id, channel);
                }
            }

            template.UsageCount += 1;
            await _templates.UpdateAsync(template);
            return await _serverService.ViewAsync(userId, serverId);
        }

        private async Task MakeChannelAsync(string userId, int serverId, int? categoryId, ServerTemplateChannelDto channel)
        {
            await _channels.CreateChannelAsync(userId, serverId, new CreateServerChannelDto
            {
                Name = channel.Name,
                Kind = channel.Kind,
                CategoryId = categoryId,
                IsPrivate = channel.IsPrivate,
                UserLimit = channel.UserLimit,
                SlowModeSeconds = channel.SlowModeSeconds
            });
        }

        private async Task<ServerTemplateSnapshotDto> TakeSnapshotAsync(string userId, int serverId)
        {
            var channels = await _channels.GetAsync(userId, serverId);
            var roles = (await _roles.GetRolesAsync(userId, serverId)).Where(r => !r.IsEveryone).OrderBy(r => r.Position).Take(MaxRoles);
            var snapshot = new ServerTemplateSnapshotDto
            {
                Roles = roles.Select(r => new ServerTemplateRoleDto { Name = r.Name, Color = r.Color, Permissions = r.Permissions, DisplaySeparately = r.DisplaySeparately }).ToList(),
                Loose = channels.Loose.OrderBy(c => c.Position).Select(ToTemplateChannel).ToList(),
                Categories = channels.Categories.OrderBy(c => c.Position).Select(c => new ServerTemplateCategoryDto
                {
                    Name = c.Name,
                    IsPrivate = c.IsPrivate,
                    Channels = c.Channels.OrderBy(x => x.Position).Select(ToTemplateChannel).ToList()
                }).ToList()
            };
            if (Count(snapshot) > MaxChannels)
            {
                throw new BadRequestException("A template can hold at most " + MaxChannels + " channels.");
            }

            return snapshot;
        }

        private static ServerTemplateChannelDto ToTemplateChannel(ServerChannelDto channel)
        {
            return new ServerTemplateChannelDto
            {
                Name = channel.Name,
                Kind = channel.Kind,
                IsPrivate = channel.IsPrivate,
                UserLimit = channel.UserLimit,
                SlowModeSeconds = channel.SlowModeSeconds
            };
        }

        private static int Count(ServerTemplateSnapshotDto snapshot)
        {
            return snapshot.Loose.Count + snapshot.Categories.Sum(c => c.Channels.Count);
        }

        private static ServerTemplateSnapshotDto Read(ServerTemplate template)
        {
            try
            {
                return JsonSerializer.Deserialize<ServerTemplateSnapshotDto>(template.Snapshot, Json) ?? new ServerTemplateSnapshotDto();
            }
            catch (JsonException)
            {
                return new ServerTemplateSnapshotDto();
            }
        }

        private async Task<ServerTemplateDto> ToDtoAsync(string userId, ServerTemplate template, bool compare)
        {
            var snapshot = Read(template);
            var outOfDate = false;
            if (compare)
            {
                var current = await TakeSnapshotAsync(userId, template.ServerId);
                outOfDate = JsonSerializer.Serialize(current, Json) != JsonSerializer.Serialize(snapshot, Json);
            }

            return new ServerTemplateDto
            {
                Code = template.Code,
                Title = template.Title,
                Description = template.Description,
                UsageCount = template.UsageCount,
                RoleCount = snapshot.Roles.Count,
                CategoryCount = snapshot.Categories.Count,
                ChannelCount = Count(snapshot),
                CreatedAt = template.CreatedAt,
                SyncedAt = template.SyncedAt,
                OutOfDate = outOfDate
            };
        }

        private async Task<string> NewCodeAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var chars = new char[12];
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
                }

                var code = new string(chars);
                if (!await _templates.CodeTakenAsync(code))
                {
                    return code;
                }
            }

            throw new BadRequestException("Could not make a template link. Try again.");
        }
    }
}
