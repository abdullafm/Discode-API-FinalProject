using Service.Helpers.DTOs.Emojis;

namespace Service.Services.Interfaces
{
    public interface IEmojiService
    {
        Task<IEnumerable<EmojiCategoryDto>> GetAllAsync();
    }
}
