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
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(string userId)
        {
            return await _conversations.GetForUserAsync(userId);
        }

        public async Task<ChatDto> GetChatAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var chat = new ChatDto
            {
                UserId = other.Id,
                UserName = other.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(other.DisplayName) ? other.UserName ?? string.Empty : other.DisplayName,
                AvatarUrl = other.AvatarUrl ?? string.Empty,
                Status = await _presence.ResolveAsync(other),
                MemberSince = other.CreatedAt,
                IsFriend = (await _friendRequests.GetBetweenAsync(userId, other.Id))?.Status == FriendRequestStatus.Accepted
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

        public async Task<SentMessageDto> SendAsync(string senderId, string userName, SendMessageDto model)
        {
            await _validator.EnsureValidAsync(model);

            var recipient = await GetChatPartnerAsync(senderId, userName, requireFriendship: true);
            var text = model.Text.Trim();

            var conversation = await _conversations.GetBetweenAsync(senderId, recipient.Id)
                               ?? await _conversations.CreateAsync(senderId, recipient.Id, Preview(text), hiddenForOther: false);

            var messageForSender = await _messages.CreateAsync(conversation.ConversationId, senderId, text);
            await _conversations.RecordMessageAsync(senderId, recipient.Id, Preview(text), messageForSender.CreatedAt);

            return new SentMessageDto
            {
                SenderId = senderId,
                RecipientId = recipient.Id,
                MessageForSender = messageForSender,
                MessageForRecipient = (await _messages.GetAsync(messageForSender.Id, recipient.Id))!,
                ConversationForSender = (await _conversations.GetBetweenAsync(senderId, recipient.Id))!,
                ConversationForRecipient = (await _conversations.GetBetweenAsync(recipient.Id, senderId))!
            };
        }

        public async Task<EditedMessageDto> EditAsync(string userId, int messageId, SendMessageDto model)
        {
            await _validator.EnsureValidAsync(model);

            var original = await _messages.GetAsync(messageId, userId);
            if (original is null || !original.Mine)
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
                await _conversations.UpdatePreviewAsync(conversation.ConversationId, Preview(text));
            }

            return new EditedMessageDto
            {
                SenderId = userId,
                RecipientId = conversation.UserId,
                MessageForSender = messageForSender,
                MessageForRecipient = (await _messages.GetAsync(messageId, conversation.UserId))!
            };
        }

        public async Task<ReactionUpdatedDto> ReactAsync(string userId, int messageId, ToggleReactionDto model)
        {
            await _reactionValidator.EnsureValidAsync(model);

            var message = await _messages.GetAsync(messageId, userId);
            if (message is null)
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
            if (other is null || other.Id == userId || await _blocks.IsBlockedEitherWayAsync(userId, other.Id))
            {
                throw new NotFoundException("User not found.");
            }

            if (requireFriendship)
            {
                var request = await _friendRequests.GetBetweenAsync(userId, other.Id);
                if (request is null || request.Status != FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("You can only message your friends.");
                }
            }

            return other;
        }

        private static string Preview(string text)
        {
            return text.Length <= PreviewLength ? text : text.Substring(0, PreviewLength);
        }
    }
}
