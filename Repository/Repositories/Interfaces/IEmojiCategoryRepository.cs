using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IEmojiCategoryRepository : IBaseRepository<EmojiCategory>
    {
        Task<IEnumerable<EmojiCategory>> GetAllWithEmojisAsync();
    }
}
