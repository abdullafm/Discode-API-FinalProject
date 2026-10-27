using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using AutoMapper;
using Service.Helpers;
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
        private readonly IServerOverwriteRepository _overwrites;
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
                                    IServerOverwriteRepository overwrites,
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
            _overwrites = overwrites;
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

            var context = await _permissions.GetContextAsync(userId, serverId);

            var result = new ServerChannelsDto();
            foreach (var channel in await _channels.GetLooseAsync(serverId))
            {
                if (CanView(context, channel))
                {
                    result.Loose.Add(ToDto(channel));
                }
            }

            foreach (var category in await _categories.GetForServerAsync(serverId))
            {
                var dto = new ServerCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    IsPrivate = category.IsPrivate,
                    Position = category.Position
                };
                foreach (var channel in category.Channels.OrderBy(c => c.Position).ThenBy(c => c.Id))
                {
                    if (CanView(context, channel))
                    {
                        dto.Channels.Add(ToDto(channel));
                    }
                }

                var categoryVisible = (context.ForCategory(category.Id) & ServerPermission.ViewChannels) == ServerPermission.ViewChannels;
                if (categoryVisible || dto.Channels.Count > 0)
                {
                    result.Categories.Add(dto);
                }
            }

            return result;
        }

        private static bool CanView(ChannelPermissionContext context, ServerChannel channel)
        {
            return (context.ForChannel(channel) & ServerPermission.ViewChannels) == ServerPermission.ViewChannels;
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
                SyncWithCategory = category is not null && !model.IsPrivate,
                Position = await _channels.NextPositionAsync(serverId, category?.Id)
            };
            await _channels.AddAsync(channel);

            await _permissions.EnsureEveryoneAsync(serverId);
            var everyoneId = (await _roles.GetEveryoneAsync(serverId))!.Id;
            if (model.IsPrivate)
            {
                var own = new List<ServerOverwrite> { new ServerOverwrite { RoleId = everyoneId, Deny = (long)ServerPermission.ViewChannels } };
                await _overwrites.ReplaceForChannelAsync(serverId, channel.Id, own);
                SetFlags(channel, own, everyoneId);
            }
            else
            {
                SetFlags(channel, (await _overwrites.GetForServerAsync(serverId)).Where(o => o.CategoryId == category?.Id && category != null), everyoneId);
            }

            await _channels.UpdateAsync(channel);
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
            if (model.IsPrivate)
            {
                await _permissions.EnsureEveryoneAsync(serverId);
                var everyoneId = (await _roles.GetEveryoneAsync(serverId))!.Id;
                await _overwrites.ReplaceForCategoryAsync(serverId, category.Id, new List<ServerOverwrite> { new ServerOverwrite { RoleId = everyoneId, Deny = (long)ServerPermission.ViewChannels } });
            }

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

            channel.Name = name;
            var wasPrivate = channel.IsPrivate;
            await ApplyChannelOverwritesAsync(serverId, channel, model.SyncWithCategory, model.Overwrites, changes);
            if (wasPrivate != channel.IsPrivate)
            {
                changes.Add(channel.IsPrivate ? "Made the channel private" : "Made the channel public");
            }

            await _channels.UpdateAsync(channel);
            if (changes.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.ChannelUpdate, "Channel", channel.Id.ToString(), channel.Name, string.Join("; ", changes));
            }

            return ToDto(channel);
        }

        public async Task<ServerPermissionSetDto> GetChannelPermissionsAsync(string userId, int serverId, int channelId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);
            var channel = await GetTrackedAsync(serverId, channelId);
            await _permissions.EnsureEveryoneAsync(serverId);

            var all = await _overwrites.GetForServerAsync(serverId);
            var canSync = channel.CategoryId is not null;
            var synced = canSync && channel.SyncWithCategory;
            var set = synced ? all.Where(o => o.CategoryId == channel.CategoryId) : all.Where(o => o.ChannelId == channel.Id);
            var result = await DescribeAsync(serverId, set);
            result.CanSync = canSync;
            result.Synced = synced;
            return result;
        }

        public async Task<ServerPermissionSetDto> GetCategoryPermissionsAsync(string userId, int serverId, int categoryId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);
            var category = await _categories.GetTrackedAsync(serverId, categoryId) ?? throw new NotFoundException("That category was not found.");
            await _permissions.EnsureEveryoneAsync(serverId);

            var all = await _overwrites.GetForServerAsync(serverId);
            return await DescribeAsync(serverId, all.Where(o => o.CategoryId == category.Id));
        }

        public async Task<ServerOverwriteTargetsDto> SearchTargetsAsync(string userId, int serverId, string? search)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageChannels);

            var term = (search ?? string.Empty).Trim();
            var result = new ServerOverwriteTargetsDto();
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

        private async Task<ServerPermissionSetDto> DescribeAsync(int serverId, IEnumerable<ServerOverwrite> set)
        {
            var items = set.ToList();
            var roles = (await _roles.GetForServerAsync(serverId)).ToDictionary(r => r.Id);
            var everyone = roles.Values.First(r => r.IsEveryone);
            var members = (await _members.GetByUserIdsAsync(serverId, items.Where(o => o.UserId is not null).Select(o => o.UserId!).ToList())).ToDictionary(m => m.UserId);

            var result = new ServerPermissionSetDto { EveryoneRoleId = everyone.Id };
            var own = items.FirstOrDefault(o => o.RoleId == everyone.Id);
            result.Overwrites.Add(new ServerOverwriteDto
            {
                TargetType = "role",
                TargetId = everyone.Id.ToString(),
                Name = "@everyone",
                Color = everyone.Color,
                IsEveryone = true,
                Allow = own?.Allow ?? 0,
                Deny = own?.Deny ?? 0
            });

            foreach (var item in items)
            {
                if (item.RoleId is not null && item.RoleId != everyone.Id && roles.TryGetValue(item.RoleId.Value, out var role))
                {
                    result.Overwrites.Add(new ServerOverwriteDto { TargetType = "role", TargetId = role.Id.ToString(), Name = role.Name, Color = role.Color, Allow = item.Allow, Deny = item.Deny });
                }
                else if (item.UserId is not null && members.TryGetValue(item.UserId, out var member))
                {
                    var user = ToMember(member);
                    result.Overwrites.Add(new ServerOverwriteDto { TargetType = "member", TargetId = user.Id, Name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName, AvatarUrl = user.AvatarUrl, Allow = item.Allow, Deny = item.Deny });
                }
            }

            return result;
        }

        private async Task<List<ServerOverwrite>> BuildOverwritesAsync(int serverId, List<ServerOverwriteInputDto> input)
        {
            var roleIds = (await _roles.GetForServerAsync(serverId)).Select(r => r.Id).ToHashSet();
            var wantedUsers = input.Where(i => i.UserId is not null).Select(i => i.UserId!).Distinct().ToList();
            var memberIds = (await _members.GetByUserIdsAsync(serverId, wantedUsers)).Select(m => m.UserId).ToHashSet();
            var mask = (long)ChannelPermissionContext.ChannelBits;

            var items = new List<ServerOverwrite>();
            var seen = new HashSet<string>();
            foreach (var entry in input)
            {
                var hasRole = entry.RoleId is not null && roleIds.Contains(entry.RoleId.Value);
                var hasUser = entry.UserId is not null && memberIds.Contains(entry.UserId);
                if (hasRole == hasUser)
                {
                    continue;
                }

                var key = hasRole ? "r" + entry.RoleId : "u" + entry.UserId;
                var deny = entry.Deny & mask;
                var allow = entry.Allow & mask & ~deny;
                if ((allow == 0 && deny == 0) || !seen.Add(key))
                {
                    continue;
                }

                items.Add(new ServerOverwrite { RoleId = hasRole ? entry.RoleId : null, UserId = hasUser ? entry.UserId : null, Allow = allow, Deny = deny });
            }

            return items;
        }

        private static string Signature(IEnumerable<ServerOverwrite> items)
        {
            return string.Join("|", items.Select(o => (o.RoleId is not null ? "r" + o.RoleId : "u" + o.UserId) + ":" + o.Allow + ":" + o.Deny).OrderBy(x => x));
        }

        private static void SetFlags(ServerChannel channel, IEnumerable<ServerOverwrite> set, int? everyoneId)
        {
            var everyone = set.FirstOrDefault(o => o.RoleId is not null && o.RoleId == everyoneId);
            channel.IsPrivate = everyone is not null && (everyone.Deny & (long)ServerPermission.ViewChannels) != 0;
            channel.IsReadOnly = channel.Kind != ServerChannelKind.Voice && everyone is not null && (everyone.Deny & (long)ServerPermission.SendMessages) != 0;
        }

        private async Task ApplyChannelOverwritesAsync(int serverId, ServerChannel channel, bool? sync, List<ServerOverwriteInputDto>? input, List<string> changes)
        {
            await _permissions.EnsureEveryoneAsync(serverId);
            var everyoneId = (await _roles.GetEveryoneAsync(serverId))!.Id;
            var all = await _overwrites.GetForServerAsync(serverId);
            var own = all.Where(o => o.ChannelId == channel.Id).ToList();
            var fromCategory = channel.CategoryId is null ? new List<ServerOverwrite>() : all.Where(o => o.CategoryId == channel.CategoryId).ToList();

            var wantSync = channel.CategoryId is not null && (sync ?? channel.SyncWithCategory);
            if (wantSync)
            {
                if (!channel.SyncWithCategory)
                {
                    changes.Add("Synced the channel with its category");
                }

                channel.SyncWithCategory = true;
                await _overwrites.ReplaceForChannelAsync(serverId, channel.Id, new List<ServerOverwrite>());
                SetFlags(channel, fromCategory, everyoneId);
                return;
            }

            List<ServerOverwrite> items;
            if (input is not null)
            {
                items = await BuildOverwritesAsync(serverId, input);
            }
            else if (channel.SyncWithCategory && channel.CategoryId is not null)
            {
                items = fromCategory.Select(o => new ServerOverwrite { RoleId = o.RoleId, UserId = o.UserId, Allow = o.Allow, Deny = o.Deny }).ToList();
            }
            else
            {
                SetFlags(channel, own, everyoneId);
                return;
            }

            var before = channel.SyncWithCategory && channel.CategoryId is not null ? fromCategory : own;
            if (channel.SyncWithCategory && channel.CategoryId is not null)
            {
                changes.Add("Stopped syncing the channel with its category");
            }

            if (Signature(before) != Signature(items))
            {
                changes.Add("Changed the channel permissions");
            }

            channel.SyncWithCategory = false;
            await _overwrites.ReplaceForChannelAsync(serverId, channel.Id, items);
            SetFlags(channel, items, everyoneId);
        }

        private async Task ApplyCategoryOverwritesAsync(int serverId, ServerCategory category, List<ServerOverwriteInputDto> input, List<string> changes)
        {
            await _permissions.EnsureEveryoneAsync(serverId);
            var everyoneId = (await _roles.GetEveryoneAsync(serverId))!.Id;
            var before = (await _overwrites.GetForServerAsync(serverId)).Where(o => o.CategoryId == category.Id).ToList();
            var items = await BuildOverwritesAsync(serverId, input);
            if (Signature(before) == Signature(items))
            {
                return;
            }

            await _overwrites.ReplaceForCategoryAsync(serverId, category.Id, items);
            changes.Add("Changed the category permissions");

            var everyone = items.FirstOrDefault(o => o.RoleId == everyoneId);
            var nowPrivate = everyone is not null && (everyone.Deny & (long)ServerPermission.ViewChannels) != 0;
            if (nowPrivate != category.IsPrivate)
            {
                changes.Add(nowPrivate ? "Made the category private" : "Made the category public");
            }

            category.IsPrivate = nowPrivate;
            foreach (var channel in await _channels.GetByCategoryAsync(category.Id))
            {
                if (channel.SyncWithCategory)
                {
                    SetFlags(channel, items, everyoneId);
                    await _channels.UpdateAsync(channel);
                }
            }
        }

        private static ServerOverwriteMemberDto ToMember(ServerMember member)
        {
            var userName = member.User.UserName ?? string.Empty;
            return new ServerOverwriteMemberDto
            {
                Id = member.UserId,
                UserName = userName,
                DisplayName = string.IsNullOrWhiteSpace(member.User.DisplayName) ? userName : member.User.DisplayName,
                AvatarUrl = member.User.AvatarUrl
            };
        }

        public async Task<List<string>> GetViewerIdsAsync(string userId, int serverId, int channelId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }

            var channel = await _channels.GetInServerAsync(serverId, channelId) ?? throw new NotFoundException("That channel was not found.");
            var viewers = await _permissions.GetChannelViewerIdsAsync(serverId, channel);
            if (!viewers.Contains(userId))
            {
                throw new ForbiddenException("You can't view this channel.");
            }

            return viewers;
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

            category.Name = name;
            if (model.Overwrites is not null)
            {
                await ApplyCategoryOverwritesAsync(serverId, category, model.Overwrites, changes);
            }

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

            await _overwrites.DeleteForCategoryAsync(categoryId);
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
