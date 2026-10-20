using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
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
        private readonly IStoryLikeService _storyLikes;
        private readonly IStoryViewService _storyViews;
        private readonly INotificationService _notifications;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateStoryDto> _validator;
        private readonly IMapper _mapper;

        public StoryService(IStoryRepository stories,
                            IStoryLikeService storyLikes,
                            IStoryViewService storyViews,
                            INotificationService notifications,
                            IAccountRepository accounts,
                            IValidator<CreateStoryDto> validator,
                            IMapper mapper)
        {
            _stories = stories;
            _storyLikes = storyLikes;
            _storyViews = storyViews;
            _notifications = notifications;
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

            var story = await _stories.GetByIdAsync(storyId);
            var like = await _storyLikes.ToggleAsync(storyId, userId);
            if (story is not null)
            {
                if (like.Liked)
                {
                    await _notifications.AddAsync(NotificationType.StoryLike, userId, story.AuthorId, storyId: storyId);
                }
                else
                {
                    await _notifications.RemoveAsync(NotificationType.StoryLike, userId, story.AuthorId, storyId: storyId);
                }
            }

            return like;
        }

        public async Task<string?> RecordViewAsync(string userId, int storyId)
        {
            if (!await _stories.IsVisibleAsync(userId, storyId))
            {
                throw new NotFoundException("Story not found.");
            }

            var story = await _stories.GetByIdAsync(storyId);
            if (story is null || story.AuthorId == userId || await _storyViews.HasViewedAsync(storyId, userId))
            {
                return null;
            }

            await _storyViews.AddAsync(storyId, userId);
            return story.AuthorId;
        }

        public async Task<IEnumerable<StoryViewerDto>> GetViewersAsync(string userId, int storyId)
        {
            var story = await _stories.GetByIdAsync(storyId);
            if (story is null || story.AuthorId != userId)
            {
                throw new NotFoundException("Story not found.");
            }

            var likedBy = await _storyLikes.GetLikedUserIdsAsync(storyId);
            return await _storyViews.GetViewersAsync(storyId, likedBy);
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
