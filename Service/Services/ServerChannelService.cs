using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using AutoMapper;
using Service.Helpers.DTOs.ServerChannels;
using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Service.Services
{
    public class ServerChannelService : IServerChannelService
    {
        private readonly IServerChannelRepository _channels;
        private readonly IServerCategoryRepository _categories;
        private readonly IServerMemberRepository _members;
        private readonly IServerPermissionService _permissions;
        private readonly IServerAuditService _audit;
        private readonly IServerMessageRepository _messages;
        private readonly IServerChannelAccessRepository _access;
        private readonly IServerRoleRepository _roles;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateServerCategoryDto> _updateCategoryValidator;
        private readonly IValidator<UpdateServerChannelDto> _updateChannelValidator;
        private readonly IValidator<CreateServerChannelDto> _channelValidator;
        private readonly IValidator<CreateServerCategoryDto> _categoryValidator;

        public ServerChannelService(IServerChannelRepository channels,
                                    IServerCategoryRepository categories,
                                    IServerMemberRepository members,
                                    IServerPermissionService permissions,
                                    IServerAuditService audit,
                                    IServerMessageRepository messages,
                                    IServerChannelAccessRepository access,
                                    IServerRoleRepository roles,
                                    IMapper mapper,
                                    IValidator<UpdateServerCategoryDto> updateCategoryValidator,
                                    IValidator<UpdateServerChannelDto> updateChannelValidator,
                                    IValidator<CreateServerChannelDto> channelValidator,
                                    IValidator<CreateServerCategoryDto> categoryValidator)
        {
            _channels = channels;
            _categories = categories;
            _members = members;
            _permissions = permissions;
            _audit = audit;
            _messages = messages;
            _access = access;
            _roles = roles;
            _mapper = mapper;
            _updateCategoryValidator = updateCategoryValidator;
            _updateChannelValidator = updateChannelValidator;
            _channelValidator = channelValidator;
            _categoryValidator = categoryValidator;
        }

        public async Task EnsureDefaultsAsync(int serverId)
        {
            if (await _channels.AnyAsync(serverId) || (await _categories.GetForServerAsync(serverId)).Count > 0)
            {
                return;
            }

            var text = new ServerCategory
            {
                ServerId = serverId,
                Name = "Text Channels",
                Position = 0,
                Channels = new List<ServerChannel>
                {
                    new ServerChannel { ServerId = serverId, Name = "general", Kind = ServerChannelKind.Text, Position = 0 },
                    new ServerChannel { ServerId = serverId, Name = "announcements", Kind = ServerChannelKind.Announcement, Position = 1 }
                }
            };
            var voice = new ServerCategory
            {
                ServerId = serverId,
                Name = "Voice Channels",
                Position = 1,
                Channels = new List<ServerChannel>
                {
                    new ServerChannel { ServerId = serverId, Name = "Lounge", Kind = ServerChannelKind.Voice, Position = 0 }
                }
            };
            await _categories.AddAsync(text);
            await _categories.AddAsync(voice);
        }

        public async Task<ServerChannelsDto> GetAsync(string userId, int serverId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }

            await EnsureDefaultsAsync(serverId);
            var sees = (await _permissions.GetAsync(userId, serverId) & ServerPermission.ManageChannels) == ServerPermission.ManageChannels;
            var granted = sees ? new HashSet<int>() : await _access.GetChannelIdsForAsync(serverId, userId);

            var result = new ServerChannelsDto();
            foreach (var channel in await _channels.GetLooseAsync(serverId))
            {
                if (channel.IsPrivate && !sees && !granted.Contains(channel.Id))
                {
                    continue;
                }

                result.Loose.Add(ToDto(channel));
            }

            foreach (var category in await _categories.GetForServerAsync(serverId))
            {
                if (category.IsPrivate && !sees)
                {
                    continue;
                }

                var dto = new ServerCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    IsPrivate = category.IsPrivate,
                    Position = category.Position
                };
                foreach (var channel in category.Channels.OrderBy(c => c.Position).ThenBy(c => c.Id))
                {
                    if (channel.IsPrivate && !sees && !granted.Contains(channel.Id))
                    {
                        continue;
                    }

                    dto.Channels.Add(ToDto(channel));
                }

                result.Categories.Add(dto);
            }

            return result;
        }

        public async Task<ServerChannelDto> CreateChannelAsync(string userId, int serverId, CreateServerChannelDto model)
        {
            await _channelValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            ServerCategory? category = null;
            if (model.CategoryId is not null)
            {
                category = await _categories.GetInServerAsync(serverId, model.CategoryId.Value);
                if (category is null)
                {
                    throw new NotFoundException("That category was not found.");
                }
            }

            var kind = ParseKind(model.Kind);
            var name = Clean(kind, model.Name);
            if (string.IsNullOrEmpty(name))
            {
                throw new BadRequestException("Enter a channel name.");
            }

            var channel = new ServerChannel
            {
                ServerId = serverId,
                CategoryId = category?.Id,
                Name = name,
                Kind = kind,
                IsPrivate = model.IsPrivate || (category?.IsPrivate ?? false),
                Position = await _channels.NextPositionAsync(serverId, category?.Id)
            };
            await _channels.AddAsync(channel);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.ChannelCreate, "Channel", channel.Id.ToString(), channel.Name, DescribeCreate(channel, category));
            return ToDto(channel);
        }

        public async Task<ServerCategoryDto> CreateCategoryAsync(string userId, int serverId, CreateServerCategoryDto model)
        {
            await _categoryValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var category = new ServerCategory
            {
                ServerId = serverId,
                Name = model.Name.Trim(),
                IsPrivate = model.IsPrivate,
                Position = await _categories.NextPositionAsync(serverId)
            };
            await _categories.AddAsync(category);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.CategoryCreate, "Category", category.Id.ToString(), category.Name, category.IsPrivate ? "Created a private category" : "Created a category");
            return new ServerCategoryDto { Id = category.Id, Name = category.Name, IsPrivate = category.IsPrivate, Position = category.Position };
        }

        public async Task<ServerChannelDto> UpdateChannelAsync(string userId, int serverId, int channelId, UpdateServerChannelDto model)
        {
            await _updateChannelValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var channel = await GetTrackedAsync(serverId, channelId);
            var changes = new List<string>();
            var name = Clean(channel.Kind, model.Name);
            if (string.IsNullOrEmpty(name))
            {
                throw new BadRequestException("Enter a channel name.");
            }

            if (channel.Name != name)
            {
                changes.Add("Renamed the channel from \"" + channel.Name + "\" to \"" + name + "\"");
            }

            if (channel.IsPrivate != model.IsPrivate)
            {
                changes.Add(model.IsPrivate ? "Made the channel private" : "Made the channel public");
            }

            if (channel.Kind != ServerChannelKind.Voice && channel.IsReadOnly != model.IsReadOnly)
            {
                changes.Add(model.IsReadOnly ? "Stopped members from sending messages" : "Let members send messages");
            }

            channel.Name = name;
            channel.IsPrivate = model.IsPrivate;
            if (channel.Kind != ServerChannelKind.Voice)
            {
                channel.IsReadOnly = model.IsReadOnly;
            }

            await _channels.UpdateAsync(channel);

            if (!model.IsPrivate)
            {
                await _access.DeleteForChannelAsync(channel.Id);
            }
            else if (model.AccessRoleIds is not null || model.AccessUserIds is not null)
            {
                await ReplaceAccessAsync(serverId, channel, model, changes);
            }

            if (changes.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.ChannelUpdate, "Channel", channel.Id.ToString(), channel.Name, string.Join("; ", changes));
            }

            return ToDto(channel);
        }

        public async Task<ServerChannelAccessDto> GetAccessAsync(string userId, int serverId, int channelId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);
            await GetTrackedAsync(serverId, channelId);

            var entries = await _access.GetForChannelAsync(channelId);
            var roleIds = entries.Where(x => x.RoleId is not null).Select(x => x.RoleId!.Value).ToHashSet();
            var userIds = entries.Where(x => x.UserId is not null).Select(x => x.UserId!).ToList();

            var result = new ServerChannelAccessDto();
            foreach (var role in await _roles.GetForServerAsync(serverId))
            {
                if (!role.IsEveryone && roleIds.Contains(role.Id))
                {
                    result.Roles.Add(_mapper.Map<ServerRoleRefDto>(role));
                }
            }

            foreach (var member in await _members.GetByUserIdsAsync(serverId, userIds))
            {
                result.Members.Add(ToMember(member));
            }

            return result;
        }

        public async Task<ServerChannelAccessDto> SearchAccessAsync(string userId, int serverId, string? search)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var term = (search ?? string.Empty).Trim();
            var result = new ServerChannelAccessDto();
            foreach (var role in (await _roles.GetForServerAsync(serverId)).Where(r => !r.IsEveryone))
            {
                if (term.Length == 0 || role.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    result.Roles.Add(_mapper.Map<ServerRoleRefDto>(role));
                }
            }

            result.Roles = result.Roles.Take(10).ToList();
            foreach (var member in await _members.SearchAsync(serverId, term, 10))
            {
                result.Members.Add(ToMember(member));
            }

            return result;
        }

        private async Task ReplaceAccessAsync(int serverId, ServerChannel channel, UpdateServerChannelDto model, List<string> changes)
        {
            var before = await _access.GetForChannelAsync(channel.Id);
            var beforeRoles = before.Where(x => x.RoleId is not null).Select(x => x.RoleId!.Value).ToHashSet();
            var beforeUsers = before.Where(x => x.UserId is not null).Select(x => x.UserId!).ToHashSet();

            var serverRoles = (await _roles.GetForServerAsync(serverId)).Where(r => !r.IsEveryone).Select(r => r.Id).ToHashSet();
            var roleIds = (model.AccessRoleIds ?? beforeRoles.ToList()).Where(serverRoles.Contains).Distinct().ToList();

            var wantedUsers = (model.AccessUserIds ?? beforeUsers.ToList()).Distinct().ToList();
            var userIds = (await _members.GetByUserIdsAsync(serverId, wantedUsers)).Select(m => m.UserId).ToList();

            if (beforeRoles.SetEquals(roleIds) && beforeUsers.SetEquals(userIds))
            {
                return;
            }

            await _access.ReplaceAsync(channel.Id, roleIds, userIds);
            changes.Add("Changed who can access the channel (" + roleIds.Count + " roles, " + userIds.Count + " members)");
        }

        private static ServerChannelAccessMemberDto ToMember(ServerMember member)
        {
            var userName = member.User.UserName ?? string.Empty;
            return new ServerChannelAccessMemberDto
            {
                Id = member.UserId,
                UserName = userName,
                DisplayName = string.IsNullOrWhiteSpace(member.User.DisplayName) ? userName : member.User.DisplayName,
                AvatarUrl = member.User.AvatarUrl
            };
        }

        public async Task DeleteChannelAsync(string userId, int serverId, int channelId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var channel = await GetTrackedAsync(serverId, channelId);
            await _messages.DeleteForChannelsAsync(new[] { channelId });
            await _channels.DeleteAsync(channel);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.ChannelDelete, "Channel", channelId.ToString(), channel.Name, "Deleted the channel");
        }

        private async Task<ServerChannel> GetTrackedAsync(int serverId, int channelId)
        {
            var channel = await _channels.GetByIdAsync(channelId);
            if (channel is null || channel.ServerId != serverId)
            {
                throw new NotFoundException("That channel was not found.");
            }

            return channel;
        }

        public async Task<ServerCategoryDto> UpdateCategoryAsync(string userId, int serverId, int categoryId, UpdateServerCategoryDto model)
        {
            await _updateCategoryValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var category = await _categories.GetTrackedAsync(serverId, categoryId);
            if (category is null)
            {
                throw new NotFoundException("That category was not found.");
            }

            var changes = new List<string>();
            var name = model.Name.Trim();
            if (category.Name != name)
            {
                changes.Add("Renamed the category from \"" + category.Name + "\" to \"" + name + "\"");
            }

            if (category.IsPrivate != model.IsPrivate)
            {
                changes.Add(model.IsPrivate ? "Made the category private" : "Made the category public");
            }

            category.Name = name;
            category.IsPrivate = model.IsPrivate;
            await _categories.UpdateAsync(category);
            if (changes.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.CategoryUpdate, "Category", category.Id.ToString(), category.Name, string.Join("; ", changes));
            }

            return new ServerCategoryDto { Id = category.Id, Name = category.Name, IsPrivate = category.IsPrivate, Position = category.Position };
        }

        public async Task DeleteCategoryAsync(string userId, int serverId, int categoryId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var category = await _categories.GetTrackedAsync(serverId, categoryId);
            if (category is null)
            {
                throw new NotFoundException("That category was not found.");
            }

            var channelIds = await _channels.GetIdsInCategoryAsync(categoryId);
            if (channelIds.Count > 0)
            {
                await _messages.DeleteForChannelsAsync(channelIds);
                await _channels.DeleteInCategoryAsync(categoryId);
            }

            await _categories.DeleteAsync(category);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.CategoryDelete, "Category", categoryId.ToString(), category.Name, channelIds.Count == 0 ? "Deleted the category" : "Deleted the category and its " + channelIds.Count + (channelIds.Count == 1 ? " channel" : " channels"));
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
        }

        private static ServerChannelKind ParseKind(string kind)
        {
            return kind.Trim().ToLowerInvariant() switch
            {
                "voice" => ServerChannelKind.Voice,
                "announce" => ServerChannelKind.Announcement,
                _ => ServerChannelKind.Text
            };
        }

        private static string Clean(ServerChannelKind kind, string value)
        {
            var text = Regex.Replace(value.Trim(), @"\s+", " ");
            if (kind == ServerChannelKind.Voice)
            {
                return text;
            }

            var slug = Regex.Replace(text.ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-_]", string.Empty);
            return slug.Length == 0 ? "new-channel" : slug;
        }

        private static string DescribeCreate(ServerChannel channel, ServerCategory? category)
        {
            var type = channel.Kind == ServerChannelKind.Voice ? "voice" : channel.Kind == ServerChannelKind.Announcement ? "announcement" : "text";
            var text = "Created a " + (channel.IsPrivate ? "private " : string.Empty) + type + " channel";
            return category is null ? text : text + " in " + category.Name;
        }

        private static ServerChannelDto ToDto(ServerChannel channel)
        {
            return new ServerChannelDto
            {
                Id = channel.Id,
                CategoryId = channel.CategoryId,
                Name = channel.Name,
                Kind = channel.Kind == ServerChannelKind.Voice ? "voice" : channel.Kind == ServerChannelKind.Announcement ? "announce" : "text",
                IsPrivate = channel.IsPrivate,
                IsReadOnly = channel.IsReadOnly,
                Position = channel.Position
            };
        }
    }
}
