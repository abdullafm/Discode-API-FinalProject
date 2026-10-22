using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class DirectMessageService : IDirectMessageService
    {
        private const int HistoryLimit = 100;
        private const int PreviewLength = 150;
        private const string CallPreview = "Started a call";
        private const int MaxPinnedMessages = 50;

        private readonly IConversationService _conversations;
        private readonly IMessageService _messages;
        private readonly IDirectMessageRepository _directMessages;
        private readonly IMessageReactionService _reactions;
        private readonly IAccountRepository _accounts;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IUserBlockRepository _blocks;
        private readonly IPresenceService _presence;
        private readonly IValidator<SendMessageDto> _validator;
        private readonly IValidator<ToggleReactionDto> _reactionValidator;
        private readonly IValidator<ForwardMessageDto> _forwardValidator;
        private readonly IValidator<CreatePollDto> _pollValidator;
        private readonly IPollRepository _polls;
        private readonly IMapper _mapper;

        public DirectMessageService(IConversationService conversations,
                                    IMessageService messages,
                                    IDirectMessageRepository directMessages,
                                    IMessageReactionService reactions,
                                    IAccountRepository accounts,
                                    IFriendRequestRepository friendRequests,
                                    IUserBlockRepository blocks,
                                    IPresenceService presence,
                                    IValidator<SendMessageDto> validator,
                                    IValidator<ToggleReactionDto> reactionValidator,
                                    IValidator<ForwardMessageDto> forwardValidator,
                                    IValidator<CreatePollDto> pollValidator,
                                    IPollRepository polls,
                                    IMapper mapper)
        {
            _conversations = conversations;
            _messages = messages;
            _directMessages = directMessages;
            _reactions = reactions;
            _accounts = accounts;
            _friendRequests = friendRequests;
            _blocks = blocks;
            _presence = presence;
            _validator = validator;
            _reactionValidator = reactionValidator;
            _forwardValidator = forwardValidator;
            _pollValidator = pollValidator;
            _polls = polls;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(string userId)
        {
            return await _conversations.GetForUserAsync(userId);
        }

        public async Task<ChatDto> GetChatAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var friendship = await _friendRequests.GetBetweenAsync(userId, other.Id);
            var myBlock = await _blocks.GetBlockAsync(userId, other.Id);
            var isFriend = friendship?.Status == FriendRequestStatus.Accepted;
            var showDetails = other.IsPublic || isFriend;
            var presence = await _presence.ResolveAsync(other);
            var chat = new ChatDto
            {
                Bio = showDetails ? other.Bio ?? string.Empty : string.Empty,
                Pronouns = showDetails ? other.Pronouns ?? string.Empty : string.Empty,
                CustomStatus = showDetails && presence != "offline" ? other.CustomStatus ?? string.Empty : string.Empty,
                UserId = other.Id,
                UserName = other.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(other.DisplayName) ? other.UserName ?? string.Empty : other.DisplayName,
                AvatarUrl = other.AvatarUrl ?? string.Empty,
                Status = presence,
                MemberSince = other.CreatedAt,
                IsFriend = isFriend,
                FriendRequestId = friendship?.Id,
                Relation = myBlock is null ? DescribeRelation(friendship, userId) : "blocked",
                BlockId = myBlock?.Id,
                IsPrivate = !showDetails
            };

            var conversation = await _conversations.GetBetweenAsync(userId, other.Id);
            if (conversation is null)
            {
                if (!chat.IsFriend)
                {
                    return chat;
                }

                try
                {
                    conversation = await _conversations.CreateAsync(userId, other.Id, string.Empty, hiddenForOther: true);
                }
                catch (Exception)
                {
                    return chat;
                }
            }
            else
            {
                await _conversations.ShowForAsync(userId, other.Id);
            }

            var messages = await _directMessages.GetChatMessagesAsync(userId, other.Id, HistoryLimit);
            chat.Messages = _mapper.Map<List<MessageDto>>(messages, options => options.Items[MappingProfile.ViewerIdKey] = userId);
            await _conversations.ResetUnreadAsync(userId, other.Id);
            return chat;
        }

        public async Task<RelationDto> GetRelationAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var friendship = await _friendRequests.GetBetweenAsync(userId, other.Id);
            var myBlock = await _blocks.GetBlockAsync(userId, other.Id);
            return new RelationDto
            {
                Relation = myBlock is null ? DescribeRelation(friendship, userId) : "blocked",
                FriendRequestId = friendship?.Id,
                BlockId = myBlock?.Id
            };
        }

        public async Task<SentMessageDto> SendAsync(string senderId, string userName, SendMessageDto model)
        {
            await _validator.EnsureValidAsync(model);

            var recipient = await GetChatPartnerAsync(senderId, userName, requireFriendship: false);
            var undelivered = await _blocks.IsBlockedEitherWayAsync(senderId, recipient.Id) || !await CanMessageAsync(senderId, recipient);

            var text = model.Text.Trim();

            var preview = Preview(text, model.Attachments.Count);

            var conversation = await _conversations.GetBetweenAsync(senderId, recipient.Id)
                               ?? await _conversations.CreateAsync(senderId, recipient.Id, undelivered ? string.Empty : preview, hiddenForOther: undelivered);

            var messageForSender = await _messages.CreateAsync(conversation.ConversationId, senderId, text, model.ReplyToMessageId, undelivered, model.Attachments);
            if (!undelivered)
            {
                await _conversations.RecordMessageAsync(senderId, recipient.Id, preview, messageForSender.CreatedAt);
            }

            return await ToSentAsync(senderId, recipient.Id, messageForSender);
        }

        public async Task<SentMessageDto> SendPollAsync(string senderId, string userName, CreatePollDto model)
        {
            await _pollValidator.EnsureValidAsync(model);

            var recipient = await GetChatPartnerAsync(senderId, userName, requireFriendship: false);
            var undelivered = await _blocks.IsBlockedEitherWayAsync(senderId, recipient.Id) || !await CanMessageAsync(senderId, recipient);

            var preview = Preview("Poll: " + model.Question.Trim());
            var conversation = await _conversations.GetBetweenAsync(senderId, recipient.Id)
                               ?? await _conversations.CreateAsync(senderId, recipient.Id, undelivered ? string.Empty : preview, hiddenForOther: undelivered);

            var messageForSender = await _messages.CreatePollAsync(conversation.ConversationId, senderId, model, undelivered);
            if (!undelivered)
            {
                await _conversations.RecordMessageAsync(senderId, recipient.Id, preview, messageForSender.CreatedAt);
            }

            return await ToSentAsync(senderId, recipient.Id, messageForSender);
        }

        public async Task<PollUpdatedDto> VotePollAsync(string userId, int messageId, VotePollDto model)
        {
            var message = await _messages.GetAsync(messageId, userId);
            if (message is null || !message.IsPoll)
            {
                throw new NotFoundException("Poll not found.");
            }

            var conversation = await _conversations.GetByIdAsync(message.ConversationId, userId);
            if (conversation is null || await _blocks.IsBlockedEitherWayAsync(userId, conversation.UserId))
            {
                throw new NotFoundException("Poll not found.");
            }

            var poll = await _polls.GetByMessageIdAsync(messageId);
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

            var forUser = await _messages.GetAsync(messageId, userId);
            var forRecipient = await _messages.GetAsync(messageId, conversation.UserId);
            return new PollUpdatedDto
            {
                UserId = userId,
                RecipientId = conversation.UserId,
                MessageId = messageId,
                ForUser = forUser!.Poll!,
                ForRecipient = forRecipient!.Poll!
            };
        }

        private async Task<SentMessageDto> ToSentAsync(string senderId, string recipientId, MessageDto messageForSender)
        {
            return new SentMessageDto
            {
                SenderId = senderId,
                RecipientId = recipientId,
                MessageForSender = messageForSender,
                MessageForRecipient = (await _messages.GetAsync(messageForSender.Id, recipientId))!,
                ConversationForSender = (await _conversations.GetBetweenAsync(senderId, recipientId))!,
                ConversationForRecipient = (await _conversations.GetBetweenAsync(recipientId, senderId))!
            };
        }

        public async Task<EditedMessageDto> EditAsync(string userId, int messageId, SendMessageDto model)
        {
            await _validator.EnsureValidAsync(model);

            var original = await _messages.GetAsync(messageId, userId);
            if (original is null || !original.Mine || original.IsCall || original.IsPoll)
            {
                throw new NotFoundException("Message not found.");
            }

            var conversation = await _conversations.GetByIdAsync(original.ConversationId, userId);
            if (conversation is null || await _blocks.IsBlockedEitherWayAsync(userId, conversation.UserId))
            {
                throw new NotFoundException("Message not found.");
            }

            var text = model.Text.Trim();
            var messageForSender = await _messages.EditAsync(messageId, text, userId);
            if (messageForSender is null)
            {
                throw new NotFoundException("Message not found.");
            }

            if (await _messages.GetLatestIdAsync(conversation.ConversationId) == messageId)
            {
                await _conversations.UpdatePreviewAsync(conversation.ConversationId, Preview(text, original.Attachments.Count));
            }

            return new EditedMessageDto
            {
                SenderId = userId,
                RecipientId = conversation.UserId,
                MessageForSender = messageForSender,
                MessageForRecipient = (await _messages.GetAsync(messageId, conversation.UserId))!
            };
        }

        public async Task<IEnumerable<SentMessageDto>> ForwardAsync(string userId, int messageId, ForwardMessageDto model)
        {
            await _forwardValidator.EnsureValidAsync(model);

            var message = await _messages.GetAsync(messageId, userId);
            if (message is null || message.IsCall || message.IsPoll)
            {
                throw new NotFoundException("Message not found.");
            }

            var conversation = await _conversations.GetByIdAsync(message.ConversationId, userId);
            if (conversation is null)
            {
                throw new NotFoundException("Message not found.");
            }

            var originalSenderId = message.Mine ? userId : conversation.UserId;
            return await ForwardContentAsync(userId, originalSenderId, message.Text, message.Attachments, model);
        }

        public async Task<IEnumerable<SentMessageDto>> ForwardContentAsync(string userId, string originalSenderId, string text, IEnumerable<MessageAttachmentDto> attachments, ForwardMessageDto model)
        {
            await _forwardValidator.EnsureValidAsync(model);

            var userNames = model.UserNames.Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var recipients = new List<AppUser>();
            foreach (var userName in userNames)
            {
                var recipient = await GetChatPartnerAsync(userId, userName, requireFriendship: true);
                if (await _blocks.IsBlockedEitherWayAsync(userId, recipient.Id))
                {
                    throw new NotFoundException("User not found.");
                }

                recipients.Add(recipient);
            }

            var note = model.Note.Trim();
            var sent = new List<SentMessageDto>();
            foreach (var recipient in recipients)
            {
                var target = await _conversations.GetBetweenAsync(userId, recipient.Id)
                             ?? await _conversations.CreateAsync(userId, recipient.Id, Preview(text, attachments.Count()), hiddenForOther: false);

                var forwarded = await _directMessages.ForwardAsync(new MessageForward
                {
                    OriginalSenderId = originalSenderId,
                    Message = new Message
                    {
                        ConversationId = target.ConversationId,
                        SenderId = userId,
                        Text = text,
                        Attachments = attachments
                            .Select(a => new MessageAttachment { Url = a.Url, FileName = a.FileName, ContentType = a.ContentType, Size = a.Size })
                            .ToList()
                    }
                });
                await _conversations.RecordMessageAsync(userId, recipient.Id, Preview(text, attachments.Count()), forwarded.Message.CreatedAt);
                sent.Add(await ToSentAsync(userId, recipient.Id, (await _messages.GetAsync(forwarded.MessageId, userId))!));

                if (note.Length > 0)
                {
                    sent.Add(await SendAsync(userId, recipient.UserName ?? string.Empty, new SendMessageDto { Text = note }));
                }
            }

            return sent;
        }

        public async Task<SentMessageDto> StartCallMessageAsync(string callerId, string calleeId)
        {
            var conversation = await _conversations.GetBetweenAsync(callerId, calleeId)
                               ?? await _conversations.CreateAsync(callerId, calleeId, CallPreview, hiddenForOther: false);

            var message = await _messages.CreateCallAsync(conversation.ConversationId, callerId);
            await _conversations.RecordMessageAsync(callerId, calleeId, CallPreview, message.CreatedAt);
            return await ToSentAsync(callerId, calleeId, message);
        }

        public async Task<EditedMessageDto> FinishCallMessageAsync(string callerId, string calleeId, int messageId, int? seconds, bool missed)
        {
            await _messages.FinishCallAsync(messageId, seconds, missed);
            return new EditedMessageDto
            {
                SenderId = callerId,
                RecipientId = calleeId,
                MessageForSender = (await _messages.GetAsync(messageId, callerId))!,
                MessageForRecipient = (await _messages.GetAsync(messageId, calleeId))!
            };
        }

        public async Task<DeletedMessageDto> DeleteAsync(string userId, int messageId)
        {
            var message = await _messages.GetAsync(messageId, userId);
            if (message is null || !message.Mine || message.IsCall)
            {
                throw new NotFoundException("Message not found.");
            }

            var conversation = await _conversations.GetByIdAsync(message.ConversationId, userId);
            if (conversation is null)
            {
                throw new NotFoundException("Message not found.");
            }

            await _messages.DeleteAsync(messageId);

            var latestId = await _messages.GetLatestIdAsync(conversation.ConversationId);
            var latest = latestId.HasValue ? await _messages.GetAsync(latestId.Value, userId) : null;
            var preview = latest is null ? string.Empty : (latest.IsPoll && latest.Poll is not null ? Preview("Poll: " + latest.Poll.Question) : Preview(latest.Text, latest.Attachments.Count));
            await _conversations.UpdatePreviewAsync(conversation.ConversationId, preview);

            return new DeletedMessageDto
            {
                MessageId = messageId,
                ConversationId = conversation.ConversationId,
                SenderId = userId,
                RecipientId = conversation.UserId,
                Preview = preview
            };
        }

        public async Task<ReactionUpdatedDto> ReactAsync(string userId, int messageId, ToggleReactionDto model)
        {
            await _reactionValidator.EnsureValidAsync(model);

            var message = await _messages.GetAsync(messageId, userId);
            if (message is null || message.IsCall)
            {
                throw new NotFoundException("Message not found.");
            }

            var conversation = await _conversations.GetByIdAsync(message.ConversationId, userId);
            if (conversation is null || await _blocks.IsBlockedEitherWayAsync(userId, conversation.UserId))
            {
                throw new NotFoundException("Message not found.");
            }

            await _reactions.ToggleAsync(messageId, userId, model.Emoji.Trim());

            var forUser = await _messages.GetAsync(messageId, userId);
            var forRecipient = await _messages.GetAsync(messageId, conversation.UserId);

            return new ReactionUpdatedDto
            {
                UserId = userId,
                RecipientId = conversation.UserId,
                ForUser = new MessageReactionsDto { MessageId = messageId, Reactions = forUser!.Reactions },
                ForRecipient = new MessageReactionsDto { MessageId = messageId, Reactions = forRecipient!.Reactions }
            };
        }

        public async Task<PinUpdatedDto> PinAsync(string userId, int messageId, PinMessageDto model)
        {
            var message = await _messages.GetAsync(messageId, userId);
            if (message is null || message.IsCall)
            {
                throw new NotFoundException("Message not found.");
            }

            var conversation = await _conversations.GetByIdAsync(message.ConversationId, userId);
            if (conversation is null || await _blocks.IsBlockedEitherWayAsync(userId, conversation.UserId))
            {
                throw new NotFoundException("Message not found.");
            }

            if (model.Pinned && message.PinnedAt is null && await _messages.CountPinnedAsync(conversation.ConversationId) >= MaxPinnedMessages)
            {
                throw new BadRequestException("You can pin at most 50 messages in a chat.");
            }

            await _messages.SetPinnedAsync(messageId, model.Pinned);

            return new PinUpdatedDto
            {
                UserId = userId,
                RecipientId = conversation.UserId,
                ForUser = (await _messages.GetAsync(messageId, userId))!,
                ForRecipient = (await _messages.GetAsync(messageId, conversation.UserId))!
            };
        }

        public async Task<IEnumerable<MessageDto>> GetPinsAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var conversation = await _conversations.GetBetweenAsync(userId, other.Id);
            if (conversation is null)
            {
                return new List<MessageDto>();
            }

            return await _messages.GetPinnedAsync(conversation.ConversationId, userId);
        }

        public async Task MarkReadAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            await _conversations.ResetUnreadAsync(userId, other.Id);
        }

        public async Task CloseAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            await _conversations.HideForAsync(userId, other.Id);
        }

        private async Task<AppUser> GetChatPartnerAsync(string userId, string userName, bool requireFriendship)
        {
            var other = await _accounts.GetByUserNameAsync(userName.Trim());
            if (other is null || other.Id == userId)
            {
                throw new NotFoundException("User not found.");
            }

            if (requireFriendship)
            {
                await EnsureCanMessageAsync(userId, other);
            }

            return other;
        }

        private async Task<bool> CanMessageAsync(string userId, AppUser other)
        {
            if (other.IsPublic)
            {
                return true;
            }

            var request = await _friendRequests.GetBetweenAsync(userId, other.Id);
            return request is not null && request.Status == FriendRequestStatus.Accepted;
        }

        private async Task EnsureCanMessageAsync(string userId, AppUser other)
        {
            if (!await CanMessageAsync(userId, other))
            {
                throw new BadRequestException("This profile is private. You can message them once you are friends.");
            }
        }

        private static string DescribeRelation(FriendRequest? friendship, string userId)
        {
            if (friendship is null)
            {
                return "none";
            }

            if (friendship.Status == FriendRequestStatus.Accepted)
            {
                return "friend";
            }

            return friendship.SenderId == userId ? "pending-sent" : "pending-received";
        }

        private static string Preview(string text)
        {
            return text.Length <= PreviewLength ? text : text.Substring(0, PreviewLength);
        }

        private static string Preview(string text, int attachments)
        {
            if (text.Length == 0 && attachments > 0)
            {
                return attachments == 1 ? "Sent an attachment" : "Sent " + attachments + " attachments";
            }

            return Preview(text);
        }
    }
}
