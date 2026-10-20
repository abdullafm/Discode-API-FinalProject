using AutoMapper;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messages;
        private readonly IMapper _mapper;

        public MessageService(IMessageRepository messages, IMapper mapper)
        {
            _messages = messages;
            _mapper = mapper;
        }

        public async Task<MessageDto?> GetAsync(int messageId, string viewerId)
        {
            var message = await _messages.GetWithDetailsAsync(messageId);
            return message is null ? null : _mapper.Map<MessageDto>(message, ForViewer(viewerId));
        }

        public async Task<MessageDto> CreateAsync(int conversationId, string senderId, string text, int? replyToMessageId)
        {
            if (replyToMessageId.HasValue)
            {
                var target = await _messages.GetWithDetailsAsync(replyToMessageId.Value);
                if (target is null || target.ConversationId != conversationId)
                {
                    throw new BadRequestException("You can only reply to a message in this chat.");
                }
            }

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Text = text,
                ReplyToId = replyToMessageId
            };
            await _messages.AddAsync(message);

            var saved = await _messages.GetWithDetailsAsync(message.Id);
            return _mapper.Map<MessageDto>(saved, ForViewer(senderId));
        }

        public async Task<MessageDto?> EditAsync(int messageId, string text, string viewerId)
        {
            var message = await _messages.GetWithDetailsAsync(messageId);
            if (message is null)
            {
                return null;
            }

            message.Text = text;
            message.EditedAt = DateTime.UtcNow;
            await _messages.UpdateAsync(message);
            return _mapper.Map<MessageDto>(message, ForViewer(viewerId));
        }

        public async Task<int?> GetLatestIdAsync(int conversationId)
        {
            var latest = (await _messages.GetLatestAsync(conversationId, 1)).LastOrDefault();
            return latest?.Id;
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
