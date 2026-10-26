using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerChannels;
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
        private readonly IValidator<CreateServerChannelDto> _channelValidator;
        private readonly IValidator<CreateServerCategoryDto> _categoryValidator;

        public ServerChannelService(IServerChannelRepository channels,
                                    IServerCategoryRepository categories,
                                    IServerMemberRepository members,
                                    IServerPermissionService permissions,
                                    IServerAuditService audit,
                                    IValidator<CreateServerChannelDto> channelValidator,
                                    IValidator<CreateServerCategoryDto> categoryValidator)
        {
            _channels = channels;
            _categories = categories;
            _members = members;
            _permissions = permissions;
            _audit = audit;
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

            var result = new ServerChannelsDto();
            foreach (var channel in await _channels.GetLooseAsync(serverId))
            {
                if (channel.IsPrivate && !sees)
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
                    if (channel.IsPrivate && !sees)
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
                Position = channel.Position
            };
        }
    }
}
