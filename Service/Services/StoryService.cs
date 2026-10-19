using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.DTOs.Stories;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class StoryService : IStoryService
    {
        private readonly IStoryRepository _stories;
        private readonly IStoryLikeRepository _storyLikes;
        private readonly IStoryViewRepository _storyViews;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateStoryDto> _validator;
        private readonly IMapper _mapper;

        public StoryService(IStoryRepository stories,
                            IStoryLikeRepository storyLikes,
                            IStoryViewRepository storyViews,
                            IAccountRepository accounts,
                            IValidator<CreateStoryDto> validator,
                            IMapper mapper)
        {
            _stories = stories;
            _storyLikes = storyLikes;
            _storyViews = storyViews;
            _accounts = accounts;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<StoryDto> CreateAsync(string userId, CreateStoryDto model)
        {
            await _validator.EnsureValidAsync(model);

            var author = await _accounts.GetByIdAsync(userId);
            if (author is null)
            {
                throw new NotFoundException("User not found.");
            }

            var story = new Story
            {
                AuthorId = userId,
                ImageUrl = model.ImageUrl.Trim()
            };

            await _stories.AddAsync(story);
            story.Author = author;
            return _mapper.Map<StoryDto>(story, ForViewer(userId));
        }

        public async Task<IEnumerable<StoryDto>> GetAsync(string viewerId)
        {
            await _stories.DeleteExpiredAsync(DateTime.UtcNow - StoryRules.Lifetime);
            var stories = await _stories.GetVisibleAsync(viewerId);
            return _mapper.Map<IEnumerable<StoryDto>>(stories, ForViewer(viewerId));
        }

        public async Task DeleteAsync(string userId, int storyId)
        {
            var story = await _stories.GetByIdAsync(storyId);
            if (story is null || story.AuthorId != userId)
            {
                throw new NotFoundException("Story not found.");
            }

            await _stories.DeleteAsync(story);
        }

        public async Task<LikeDto> ToggleLikeAsync(string userId, int storyId)
        {
            if (!await _stories.IsVisibleAsync(userId, storyId))
            {
                throw new NotFoundException("Story not found.");
            }

            var like = await _storyLikes.GetByUserAsync(storyId, userId);
            if (like is null)
            {
                await _storyLikes.AddAsync(new StoryLike { StoryId = storyId, UserId = userId });
            }
            else
            {
                await _storyLikes.DeleteAsync(like);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _storyLikes.CountAsync(storyId)
            };
        }

        public async Task<string?> RecordViewAsync(string userId, int storyId)
        {
            if (!await _stories.IsVisibleAsync(userId, storyId))
            {
                throw new NotFoundException("Story not found.");
            }

            var story = await _stories.GetByIdAsync(storyId);
            if (story is null || story.AuthorId == userId || await _storyViews.GetByUserAsync(storyId, userId) is not null)
            {
                return null;
            }

            await _storyViews.AddAsync(new StoryView { StoryId = storyId, UserId = userId });
            return story.AuthorId;
        }

        public async Task<IEnumerable<StoryViewerDto>> GetViewersAsync(string userId, int storyId)
        {
            var story = await _stories.GetByIdAsync(storyId);
            if (story is null || story.AuthorId != userId)
            {
                throw new NotFoundException("Story not found.");
            }

            var views = await _storyViews.GetByStoryAsync(storyId);
            var likedBy = (await _storyLikes.GetByStoryAsync(storyId)).Select(x => x.UserId).ToHashSet();

            var viewers = _mapper.Map<List<StoryViewerDto>>(views);
            foreach (var pair in views.Zip(viewers))
            {
                pair.Second.Liked = likedBy.Contains(pair.First.UserId);
            }

            return viewers;
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
