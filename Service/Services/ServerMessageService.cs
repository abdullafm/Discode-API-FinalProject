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
using System.Text.RegularExpressions;

namespace Service.Services
{
    public class ServerMessageService : IServerMessageService
    {
        private const int HistoryLimit = 50;

        private static readonly Regex EveryoneMention = new(@"(?<![\w@])@everyone\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HereMention = new(@"(?<![\w@])@here\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IServerMessageRepository _messages;
        private readonly IServerChannelRepository _channels;
        private readonly IServerMemberRepository _members;
        private readonly IServerOverwriteRepository _overwrites;
        private readonly IServerPermissionService _permissions;
        private readonly IValidator<SendServerMessageDto> _sendValidator;
        private readonly IValidator<ToggleReactionDto> _reactionValidator;
        private readonly IValidator<CreatePollDto> _pollValidator;
        private readonly IPollRepository _polls;
        private readonly IPresenceService _presence;
        private readonly IMapper _mapper;

        public ServerMessageService(IServerMessageRepository messages,
                                    IServerChannelRepository channels,
                                    IServerMemberRepository members,
                                    IServerOverwriteRepository overwrites,
                                    IServerPermissionService permissions,
                                    IValidator<SendServerMessageDto> sendValidator,
                                    IValidator<ToggleReactionDto> reactionValidator,
                                    IValidator<CreatePollDto> pollValidator,
                                    IPollRepository polls,
                                    IPresenceService presence,
                                    IMapper mapper)
        {
            _pollValidator = pollValidator;
            _polls = polls;
            _messages = messages;
            _channels = channels;
            _members = members;
            _overwrites = overwrites;
            _permissions = permissions;
            _sendValidator = sendValidator;
            _reactionValidator = reactionValidator;
            _presence = presence;
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
            await EnsureNotTimedOutAsync(userId, serverId);
            EnsureCanPost(channel, perms);

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
            var viewerIds = await ViewersAsync(serverId, channel);
            return new ServerMessageChangeDto
            {
                Message = _mapper.Map<ServerMessageDto>(saved, options => options.Items[MappingProfile.ViewerIdKey] = string.Empty),
                ViewerIds = viewerIds,
                MentionCounts = await CountMentionsAsync(userId, serverId, perms, message.Text, viewerIds)
            };
        }

        public async Task<ServerMessageChangeDto> SendPollAsync(string userId, int serverId, int channelId, CreatePollDto model)
        {
            await _pollValidator.EnsureValidAsync(model);
            var (channel, perms) = await RequireViewAsync(userId, serverId, channelId);
            await EnsureNotTimedOutAsync(userId, serverId);
            EnsureCanPost(channel, perms);
            if ((perms & ServerPermission.CreatePolls) != ServerPermission.CreatePolls)
            {
                throw new ForbiddenException("You don't have permission to create polls here.");
            }

            var message = new ServerMessage
            {
                ServerId = serverId,
                ChannelId = channelId,
                SenderId = userId,
                Text = string.Empty,
                Poll = new Poll
                {
                    Question = model.Question.Trim(),
                    AllowMultiple = model.AllowMultiple,
                    ExpiresAt = DateTime.UtcNow.AddHours(model.DurationHours),
                    Options = model.Options
                        .Select((o, index) => new PollOption { Text = o.Text.Trim(), Emoji = (o.Emoji ?? string.Empty).Trim(), Position = index })
                        .ToList()
                }
            };
            await _messages.AddAsync(message);

            var saved = await _messages.GetWithSenderAsync(message.Id);
            return new ServerMessageChangeDto
            {
                Message = _mapper.Map<ServerMessageDto>(saved, options => options.Items[MappingProfile.ViewerIdKey] = string.Empty),
                ViewerIds = await ViewersAsync(serverId, channel)
            };
        }

        public async Task<ServerMessageUpdateDto> VotePollAsync(string userId, int serverId, int channelId, int messageId, VotePollDto model)
        {
            var (channel, _) = await RequireViewAsync(userId, serverId, channelId);
            await EnsureNotTimedOutAsync(userId, serverId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.ChannelId != channelId)
            {
                throw new NotFoundException("Poll not found.");
            }

            var poll = await _polls.GetByServerMessageIdAsync(messageId);
            if (poll is null)
            {
                throw new NotFoundException("Poll not found.");
            }

            if (poll.ExpiresAt <= DateTime.UtcNow)
            {
                throw new BadRequestException("This poll has ended.");
            }

            var optionIds = (model.OptionIds ?? new List<int>()).Distinct().ToList();
            if (optionIds.Any(id => poll.Options.All(o => o.Id != id)))
            {
                throw new BadRequestException("Choose one of the answers.");
            }

            if (!poll.AllowMultiple && optionIds.Count > 1)
            {
                throw new BadRequestException("You can choose only one answer.");
            }

            await _polls.SetVotesAsync(poll, userId, optionIds);
            return await ViewsAsync(serverId, channel, messageId);
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
            await EnsureNotTimedOutAsync(userId, serverId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.ChannelId != channelId)
            {
                throw new NotFoundException("Message not found.");
            }

            await _messages.ToggleReactionAsync(messageId, userId, model.Emoji.Trim());
            return await ViewsAsync(serverId, channel, messageId);
        }

        private async Task<ServerMessageUpdateDto> ViewsAsync(int serverId, ServerChannel channel, int messageId)
        {
            var saved = await _messages.GetWithSenderAsync(messageId);
            var update = new ServerMessageUpdateDto { ServerId = serverId, ChannelId = channel.Id };
            foreach (var viewerId in await ViewersAsync(serverId, channel))
            {
                update.Views[viewerId] = _mapper.Map<ServerMessageDto>(saved, options => options.Items[MappingProfile.ViewerIdKey] = viewerId);
            }

            return update;
        }

        private static void EnsureCanPost(ServerChannel channel, ServerPermission perms)
        {
            var restricted = channel.Kind == ServerChannelKind.Announcement;
            var needed = restricted ? ServerPermission.ManageMessages : ServerPermission.SendMessages;
            if ((perms & needed) != needed)
            {
                throw new ForbiddenException(restricted
                    ? "Only people who manage messages can post in this channel."
                    : "You don't have permission to send messages here.");
            }
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

        private async Task EnsureNotTimedOutAsync(string userId, int serverId)
        {
            var membership = await _members.GetAsync(serverId, userId);
            if (membership?.TimeoutUntil is not null && membership.TimeoutUntil > DateTime.UtcNow)
            {
                throw new ForbiddenException("You are timed out and can't send messages or react right now.");
            }
        }

        private async Task<Dictionary<string, int>> CountMentionsAsync(string senderId, int serverId, ServerPermission perms, string text, List<string> viewerIds)
        {
            var everyone = EveryoneMention.IsMatch(text);
            var here = HereMention.IsMatch(text);
            if ((!everyone && !here) || (perms & ServerPermission.MentionEveryone) != ServerPermission.MentionEveryone)
            {
                return new Dictionary<string, int>();
            }

            var members = await _members.GetByUserIdsAsync(serverId, viewerIds.Where(id => id != senderId).ToList());
            var targets = new List<string>();
            foreach (var member in members)
            {
                var muted = member.MutedIndefinitely || (member.MutedUntil.HasValue && member.MutedUntil.Value > DateTime.UtcNow);
                if (muted || member.NotificationLevel == ServerNotificationLevel.Nothing)
                {
                    continue;
                }

                if (!everyone && await _presence.ResolveAsync(member.User) == "offline")
                {
                    continue;
                }

                targets.Add(member.UserId);
            }

            return await _members.AddMentionAsync(serverId, targets);
        }

        private async Task<List<string>> ViewersAsync(int serverId, ServerChannel channel)
        {
            return await _permissions.GetChannelViewerIdsAsync(serverId, channel);
        }

    }
}
