using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ConversationPinService : IConversationPinService
    {
        private readonly IConversationPinRepository _pins;

        public ConversationPinService(IConversationPinRepository pins)
        {
            _pins = pins;
        }

        public Task<List<string>> GetAsync(string userId)
        {
            return _pins.GetKeysAsync(userId);
        }

        public async Task<List<string>> ToggleAsync(string userId, string key)
        {
            key = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (key.Length == 0 || key.Length > 100)
            {
                throw new BadRequestException("Choose a conversation to pin.");
            }

            var found = await _pins.GetAsync(userId, key);
            if (found is null)
            {
                await _pins.AddAsync(new ConversationPin { UserId = userId, Key = key });
            }
            else
            {
                await _pins.DeleteAsync(found);
            }

            return await _pins.GetKeysAsync(userId);
        }
    }
}
