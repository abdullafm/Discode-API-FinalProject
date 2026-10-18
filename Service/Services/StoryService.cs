using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Stories;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class StoryService : IStoryService
    {
        private readonly IStoryRepository _stories;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateStoryDto> _validator;
        private readonly IMapper _mapper;

        public StoryService(IStoryRepository stories,
                            IAccountRepository accounts,
                            IValidator<CreateStoryDto> validator,
                            IMapper mapper)
        {
            _stories = stories;
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
            return _mapper.Map<StoryDto>(story);
        }

        public async Task<IEnumerable<StoryDto>> GetAsync(string viewerId)
        {
            var stories = await _stories.GetVisibleAsync(viewerId);
            return _mapper.Map<IEnumerable<StoryDto>>(stories);
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
    }
}
