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

        private readonly IConversationRepository _conversations;
        private readonly IMessageRepository _messages;
        private readonly IAccountRepository _accounts;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IUserBlockRepository _blocks;
        private readonly IPresenceService _presence;
        private readonly IValidator<SendMessageDto> _validator;
        private readonly IMapper _mapper;

        public DirectMessageService(IConversationRepository conversations,
                                    IMessageRepository messages,
                                    IAccountRepository accounts,
                                    IFriendRequestRepository friendRequests,
                                    IUserBlockRepository blocks,
                                    IPresenceService presence,
                                    IValidator<SendMessageDto> validator,
                                    IMapper mapper)
        {
            _conversations = conversations;
            _messages = messages;
            _accounts = accounts;
            _friendRequests = friendRequests;
            _blocks = blocks;
            _presence = presence;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(string userId)
        {
            var conversations = (await _conversations.GetForUserAsync(userId)).ToList();
            var result = new List<ConversationDto>();
            foreach (var conversation in conversations)
            {
                result.Add(await ToDtoAsync(conversation, userId));
            }

            return result;
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
                MemberSince = other.CreatedAt
            };

            var conversation = await _conversations.GetBetweenAsync(userId, other.Id);
            if (conversation is null)
            {
                return chat;
            }

            var messages = await _messages.GetLatestAsync(conversation.Id, HistoryLimit);
            chat.Messages = _mapper.Map<List<MessageDto>>(messages, ForViewer(userId));
            await ResetUnreadAsync(conversation, userId);
            return chat;
        }

        public async Task<SentMessageDto> SendAsync(string senderId, string userName, SendMessageDto model)
        {
            await _validator.EnsureValidAsync(model);

            var recipient = await GetChatPartnerAsync(senderId, userName, requireFriendship: true);
            var sender = await _accounts.GetByIdAsync(senderId);
            if (sender is null)
            {
                throw new NotFoundException("User not found.");
            }

            var text = model.Text.Trim();
            var conversation = await _conversations.GetBetweenAsync(senderId, recipient.Id);
            if (conversation is null)
            {
                var senderFirst = string.CompareOrdinal(senderId, recipient.Id) < 0;
                conversation = new Conversation
                {
                    UserAId = senderFirst ? senderId : recipient.Id,
                    UserBId = senderFirst ? recipient.Id : senderId,
                    LastSenderId = senderId,
                    LastMessageText = Preview(text)
                };
                await _conversations.AddAsync(conversation);
                conversation.UserA = senderFirst ? sender : recipient;
                conversation.UserB = senderFirst ? recipient : sender;
            }

            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                Text = text
            };
            await _messages.AddAsync(message);
            message.Sender = sender;

            conversation.LastMessageText = Preview(text);
            conversation.LastMessageAt = message.CreatedAt;
            conversation.LastSenderId = senderId;
            conversation.UserAHidden = false;
            conversation.UserBHidden = false;
            if (conversation.UserAId == recipient.Id)
            {
                conversation.UserAUnread++;
            }
            else
            {
                conversation.UserBUnread++;
            }

            await _conversations.UpdateAsync(conversation);

            return new SentMessageDto
            {
                SenderId = senderId,
                RecipientId = recipient.Id,
                MessageForSender = _mapper.Map<MessageDto>(message, ForViewer(senderId)),
                MessageForRecipient = _mapper.Map<MessageDto>(message, ForViewer(recipient.Id)),
                ConversationForSender = await ToDtoAsync(conversation, senderId),
                ConversationForRecipient = await ToDtoAsync(conversation, recipient.Id)
            };
        }

        public async Task MarkReadAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var conversation = await _conversations.GetBetweenAsync(userId, other.Id);
            if (conversation is not null)
            {
                await ResetUnreadAsync(conversation, userId);
            }
        }

        public async Task CloseAsync(string userId, string userName)
        {
            var other = await GetChatPartnerAsync(userId, userName, requireFriendship: false);
            var conversation = await _conversations.GetBetweenAsync(userId, other.Id);
            if (conversation is null)
            {
                return;
            }

            if (conversation.UserAId == userId)
            {
                conversation.UserAHidden = true;
                conversation.UserAUnread = 0;
            }
            else
            {
                conversation.UserBHidden = true;
                conversation.UserBUnread = 0;
            }

            await _conversations.UpdateAsync(conversation);
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

        private async Task ResetUnreadAsync(Conversation conversation, string userId)
        {
            var unread = conversation.UserAId == userId ? conversation.UserAUnread : conversation.UserBUnread;
            if (unread == 0)
            {
                return;
            }

            if (conversation.UserAId == userId)
            {
                conversation.UserAUnread = 0;
            }
            else
            {
                conversation.UserBUnread = 0;
            }

            await _conversations.UpdateAsync(conversation);
        }

        private async Task<ConversationDto> ToDtoAsync(Conversation conversation, string viewerId)
        {
            var dto = _mapper.Map<ConversationDto>(conversation, ForViewer(viewerId));
            var other = conversation.UserAId == viewerId ? conversation.UserB : conversation.UserA;
            dto.Status = await _presence.ResolveAsync(other);
            return dto;
        }

        private static string Preview(string text)
        {
            return text.Length <= PreviewLength ? text : text.Substring(0, PreviewLength);
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
