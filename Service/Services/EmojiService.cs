using AutoMapper;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Emojis;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class EmojiService : IEmojiService
    {
        private readonly IEmojiCategoryRepository _categories;
        private readonly IMapper _mapper;

        public EmojiService(IEmojiCategoryRepository categories, IMapper mapper)
        {
            _categories = categories;
            _mapper = mapper;
        }

        public async Task<IEnumerable<EmojiCategoryDto>> GetAllAsync()
        {
            var categories = await _categories.GetAllWithEmojisAsync();
            return _mapper.Map<IEnumerable<EmojiCategoryDto>>(categories);
        }
    }
}
