using Service.Helpers.DTOs.Emojis;

namespace Service.Services.Interfaces
{
    public interface IEmojiCategoryService
    {
        Task<IEnumerable<EmojiCategoryDto>> GetAllAsync();
    }
}
