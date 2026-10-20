using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class MessageReactionService : IMessageReactionService
    {
        private readonly IMessageReactionRepository _reactions;

        public MessageReactionService(IMessageReactionRepository reactions)
        {
            _reactions = reactions;
        }

        public async Task ToggleAsync(int messageId, string userId, string emoji)
        {
            var reaction = await _reactions.GetByUserAsync(messageId, userId, emoji);
            if (reaction is null)
            {
                await _reactions.AddAsync(new MessageReaction { MessageId = messageId, UserId = userId, Emoji = emoji });
            }
            else
            {
                await _reactions.DeleteAsync(reaction);
            }
        }
    }
}
