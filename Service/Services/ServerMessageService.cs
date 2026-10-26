using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.ServerMessages;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerMessageService : IServerMessageService
    {
        private const int HistoryLimit = 50;

        private readonly IServerMessageRepository _messages;
        private readonly IServerChannelRepository _channels;
        private readonly IServerMemberRepository _members;
        private readonly IServerOverwriteRepository _overwrites;
        private readonly IServerPermissionService _permissions;
        private readonly IValidator<SendServerMessageDto> _sendValidator;
        private readonly IValidator<ToggleReactionDto> _reactionValidator;
        private readonly IMapper _mapper;

        public ServerMessageService(IServerMessageRepository messages,
                                    IServerChannelRepository channels,
                                    IServerMemberRepository members,
                                    IServerOverwriteRepository overwrites,
                                    IServerPermissionService permissions,
                                    IValidator<SendServerMessageDto> sendValidator,
                                    IValidator<ToggleReactionDto> reactionValidator,
                                    IMapper mapper)
        {
            _messages = messages;
            _channels = channels;
            _members = members;
            _overwrites = overwrites;
            _permissions = permissions;
            _sendValidator = sendValidator;
            _reactionValidator = reactionValidator;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ServerMessageDto>> GetMessagesAsync(string userId, int serverId, int channelId, int? beforeId)
        {
            await RequireViewAsync(userId, serverId, channelId);
            var messages = await _messages.GetLatestAsync(channelId, HistoryLimit, beforeId);
            return _mapper.Map<List<ServerMessageDto>>(messages, options => options.Items[MappingProfile.ViewerIdKey] = userId);
        }

        public async Task<ServerMessageChangeDto> SendAsync(string userId, int serverId, int channelId, SendServerMessageDto model)
        {
            await _sendValidator.EnsureValidAsync(model);
            var (channel, perms) = await RequireViewAsync(userId, serverId, channelId);

            var restricted = channel.Kind == ServerChannelKind.Announcement;
            var needed = restricted ? ServerPermission.ManageMessages : ServerPermission.SendMessages;
            if ((perms & needed) != needed)
            {
                throw new ForbiddenException(restricted
                    ? "Only people who manage messages can post in this channel."
                    : "You don't have permission to send messages here.");
            }

            if (model.ReplyToMessageId.HasValue)
            {
                var target = await _messages.GetWithSenderAsync(model.ReplyToMessageId.Value);
                if (target is null || target.ChannelId != channelId)
                {
                    throw new NotFoundException("Message not found.");
                }
            }

            var message = new ServerMessage
            {
                ServerId = serverId,
                ChannelId = channelId,
                SenderId = userId,
                ReplyToMessageId = model.ReplyToMessageId,
                Text = model.Text?.Trim() ?? string.Empty,
                Attachments = model.Attachments
                    .Select(a => new ServerMessageAttachment { Url = a.Url, FileName = a.FileName, ContentType = a.ContentType, Size = a.Size })
                    .ToList()
            };
            await _messages.AddAsync(message);

            var saved = await _messages.GetWithSenderAsync(message.Id);
            return new ServerMessageChangeDto
            {
                Message = _mapper.Map<ServerMessageDto>(saved, options => options.Items[MappingProfile.ViewerIdKey] = string.Empty),
                ViewerIds = await ViewersAsync(serverId, channel)
            };
        }

        public async Task<ServerMessageDeletedDto> DeleteAsync(string userId, int serverId, int channelId, int messageId)
        {
            var (channel, perms) = await RequireViewAsync(userId, serverId, channelId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.ChannelId != channelId)
            {
                throw new NotFoundException("Message not found.");
            }

            var mine = message.SenderId == userId;
            if (!mine && (perms & ServerPermission.ManageMessages) != ServerPermission.ManageMessages)
            {
                throw new ForbiddenException("You can only delete your own messages.");
            }

            await _messages.ClearRepliesAsync(messageId);
            await _messages.DeleteAsync(await _messages.GetByIdAsync(messageId) ?? throw new NotFoundException("Message not found."));
            return new ServerMessageDeletedDto
            {
                ServerId = serverId,
                ChannelId = channelId,
                MessageId = messageId,
                ViewerIds = await ViewersAsync(serverId, channel)
            };
        }

        public async Task<ServerMessageUpdateDto> ToggleReactionAsync(string userId, int serverId, int channelId, int messageId, ToggleReactionDto model)
        {
            await _reactionValidator.EnsureValidAsync(model);
            var (channel, _) = await RequireViewAsync(userId, serverId, channelId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.ChannelId != channelId)
            {
                throw new NotFoundException("Message not found.");
            }

            await _messages.ToggleReactionAsync(messageId, userId, model.Emoji.Trim());
            var saved = await _messages.GetWithSenderAsync(messageId);
            var update = new ServerMessageUpdateDto { ServerId = serverId, ChannelId = channelId };
            foreach (var viewerId in await ViewersAsync(serverId, channel))
            {
                update.Views[viewerId] = _mapper.Map<ServerMessageDto>(saved, options => options.Items[MappingProfile.ViewerIdKey] = viewerId);
            }

            return update;
        }

        private async Task<(ServerChannel Channel, ServerPermission Permissions)> RequireViewAsync(string userId, int serverId, int channelId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }

            var channel = await _channels.GetInServerAsync(serverId, channelId);
            if (channel is null)
            {
                throw new NotFoundException("Channel not found.");
            }

            if (channel.Kind == ServerChannelKind.Voice)
            {
                throw new BadRequestException("Voice channels don't have text messages.");
            }

            var perms = await _permissions.GetInChannelAsync(userId, serverId, channel);
            if ((perms & ServerPermission.ViewChannels) != ServerPermission.ViewChannels)
            {
                throw new ForbiddenException("You can't view this channel.");
            }

            return (channel, perms);
        }

        private async Task<List<string>> ViewersAsync(int serverId, ServerChannel channel)
        {
            return await _permissions.GetChannelViewerIdsAsync(serverId, channel);
        }

    }
}
