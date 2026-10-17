using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _posts;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreatePostDto> _validator;
        private readonly IMapper _mapper;

        public PostService(IPostRepository posts,
                           IAccountRepository accounts,
                           IValidator<CreatePostDto> validator,
                           IMapper mapper)
        {
            _posts = posts;
            _accounts = accounts;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<PostDto> CreateAsync(string userId, CreatePostDto model)
        {
            await _validator.EnsureValidAsync(model);

            var author = await _accounts.GetByIdAsync(userId);
            if (author is null)
            {
                throw new NotFoundException("User not found.");
            }

            var post = new Post
            {
                AuthorId = userId,
                Text = model.Text?.Trim() ?? string.Empty,
                ImageUrl = model.ImageUrl?.Trim() ?? string.Empty
            };

            await _posts.AddAsync(post);
            post.Author = author;
            return _mapper.Map<PostDto>(post);
        }

        public async Task<IEnumerable<PostDto>> GetExploreAsync(string viewerId)
        {
            var posts = await _posts.GetExploreAsync(viewerId);
            return _mapper.Map<IEnumerable<PostDto>>(posts);
        }

        public async Task<IEnumerable<PostDto>> GetFriendsAsync(string viewerId)
        {
            var posts = await _posts.GetFriendsFeedAsync(viewerId);
            return _mapper.Map<IEnumerable<PostDto>>(posts);
        }

        public async Task<IEnumerable<PostDto>> GetByUserAsync(string viewerId, string userName)
        {
            var author = await _accounts.GetByUserNameAsync(userName.Trim());
            if (author is null)
            {
                throw new NotFoundException("User not found.");
            }

            var posts = await _posts.GetByUserAsync(viewerId, author.Id);
            return _mapper.Map<IEnumerable<PostDto>>(posts);
        }

        public async Task DeleteAsync(string userId, int postId)
        {
            var post = await _posts.GetByIdAsync(postId);
            if (post is null || post.AuthorId != userId)
            {
                throw new NotFoundException("Post not found.");
            }

            await _posts.DeleteAsync(post);
        }
    }
}
