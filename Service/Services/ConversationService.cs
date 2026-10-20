using AutoMapper;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.Mappings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversations;
        private readonly IPresenceService _presence;
        private readonly IMapper _mapper;

        public ConversationService(IConversationRepository conversations,
                                   IPresenceService presence,
                                   IMapper mapper)
        {
            _conversations = conversations;
            _presence = presence;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationDto>> GetForUserAsync(string userId)
        {
            var conversations = (await _conversations.GetForUserAsync(userId)).ToList();
            var result = new List<ConversationDto>();
            foreach (var conversation in conversations)
            {
                result.Add(await ToDtoAsync(conversation, userId));
            }

            return result;
        }

        public async Task<ConversationDto?> GetBetweenAsync(string userId, string otherId)
        {
            var conversation = await _conversations.GetBetweenAsync(userId, otherId);
            return conversation is null ? null : await ToDtoAsync(conversation, userId);
        }

        public async Task<ConversationDto?> GetByIdAsync(int conversationId, string viewerId)
        {
            var conversation = await _conversations.GetByIdAsync(conversationId);
            if (conversation is null)
            {
                return null;
            }

            if (conversation.UserAId != viewerId && conversation.UserBId != viewerId)
            {
                return null;
            }

            var otherId = conversation.UserAId == viewerId ? conversation.UserBId : conversation.UserAId;
            return await GetBetweenAsync(viewerId, otherId);
        }

        public async Task<ConversationDto> CreateAsync(string userId, string otherId, string lastMessageText, bool hiddenForOther)
        {
            var userFirst = string.CompareOrdinal(userId, otherId) < 0;
            await _conversations.AddAsync(new Conversation
            {
                UserAId = userFirst ? userId : otherId,
                UserBId = userFirst ? otherId : userId,
                LastSenderId = userId,
                LastMessageText = lastMessageText,
                UserAHidden = hiddenForOther && !userFirst,
                UserBHidden = hiddenForOther && userFirst
            });

            var created = await _conversations.GetBetweenAsync(userId, otherId);
            return await ToDtoAsync(created!, userId);
        }

        public async Task ShowForAsync(string userId, string otherId)
        {
            var conversation = await _conversations.GetBetweenAsync(userId, otherId);
            if (conversation is null)
            {
                return;
            }

            if (conversation.UserAId == userId && conversation.UserAHidden)
            {
                conversation.UserAHidden = false;
                await _conversations.UpdateAsync(conversation);
            }
            else if (conversation.UserBId == userId && conversation.UserBHidden)
            {
                conversation.UserBHidden = false;
                await _conversations.UpdateAsync(conversation);
            }
        }

        public async Task HideForAsync(string userId, string otherId)
        {
            var conversation = await _conversations.GetBetweenAsync(userId, otherId);
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

        public async Task ResetUnreadAsync(string userId, string otherId)
        {
            var conversation = await _conversations.GetBetweenAsync(userId, otherId);
            if (conversation is null)
            {
                return;
            }

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

        public async Task RecordMessageAsync(string senderId, string recipientId, string text, DateTime sentAt)
        {
            var conversation = await _conversations.GetBetweenAsync(senderId, recipientId);
            if (conversation is null)
            {
                return;
            }

            conversation.LastMessageText = text;
            conversation.LastMessageAt = sentAt;
            conversation.LastSenderId = senderId;
            conversation.UserAHidden = false;
            conversation.UserBHidden = false;
            if (conversation.UserAId == recipientId)
            {
                conversation.UserAUnread++;
            }
            else
            {
                conversation.UserBUnread++;
            }

            await _conversations.UpdateAsync(conversation);
        }

        public async Task UpdatePreviewAsync(int conversationId, string text)
        {
            var conversation = await _conversations.GetByIdAsync(conversationId);
            if (conversation is null)
            {
                return;
            }

            conversation.LastMessageText = text;
            await _conversations.UpdateAsync(conversation);
        }

        private async Task<ConversationDto> ToDtoAsync(Conversation conversation, string viewerId)
        {
            var dto = _mapper.Map<ConversationDto>(conversation, options => options.Items[MappingProfile.ViewerIdKey] = viewerId);
            var other = conversation.UserAId == viewerId ? conversation.UserB : conversation.UserA;
            dto.Status = await _presence.ResolveAsync(other);
            return dto;
        }
    }
}
