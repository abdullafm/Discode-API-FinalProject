using AutoMapper;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Stories;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class StoryViewService : IStoryViewService
    {
        private readonly IStoryViewRepository _views;
        private readonly IMapper _mapper;

        public StoryViewService(IStoryViewRepository views, IMapper mapper)
        {
            _views = views;
            _mapper = mapper;
        }

        public async Task<bool> HasViewedAsync(int storyId, string userId)
        {
            return await _views.GetByUserAsync(storyId, userId) is not null;
        }

        public async Task AddAsync(int storyId, string userId)
        {
            await _views.AddAsync(new StoryView { StoryId = storyId, UserId = userId });
        }

        public async Task<IEnumerable<StoryViewerDto>> GetViewersAsync(int storyId, ISet<string> likedUserIds)
        {
            var views = (await _views.GetByStoryAsync(storyId)).ToList();
            var viewers = _mapper.Map<List<StoryViewerDto>>(views);
            foreach (var pair in views.Zip(viewers))
            {
                pair.Second.Liked = likedUserIds.Contains(pair.First.UserId);
            }

            return viewers;
        }
    }
}
