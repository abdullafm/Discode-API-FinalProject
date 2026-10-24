using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _posts;
        private readonly IPostLikeService _postLikes;
        private readonly INotificationService _notifications;
        private readonly IAccountRepository _accounts;
        private readonly IMentionService _mentions;
        private readonly IValidator<CreatePostDto> _validator;
        private readonly IMapper _mapper;

        public PostService(IPostRepository posts,
                           IPostLikeService postLikes,
                           INotificationService notifications,
                           IAccountRepository accounts,
                           IMentionService mentions,
                           IValidator<CreatePostDto> validator,
                           IMapper mapper)
        {
            _posts = posts;
            _postLikes = postLikes;
            _notifications = notifications;
            _accounts = accounts;
            _mentions = mentions;
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
            await _mentions.NotifyAsync(NotificationType.PostMention, userId, post.Text, postId: post.Id);
            post.Author = author;
            return _mapper.Map<PostDto>(post, ForViewer(userId));
        }

        public async Task<IEnumerable<PostDto>> GetExploreAsync(string viewerId)
        {
            var posts = await _posts.GetExploreAsync(viewerId);
            return _mapper.Map<IEnumerable<PostDto>>(posts, ForViewer(viewerId));
        }

        public async Task<IEnumerable<PostDto>> GetFriendsAsync(string viewerId)
        {
            var posts = await _posts.GetFriendsFeedAsync(viewerId);
            return _mapper.Map<IEnumerable<PostDto>>(posts, ForViewer(viewerId));
        }

        public async Task<IEnumerable<PostDto>> GetByUserAsync(string viewerId, string userName)
        {
            var author = await _accounts.GetByUserNameAsync(userName.Trim());
            if (author is null)
            {
                throw new NotFoundException("User not found.");
            }

            var posts = await _posts.GetByUserAsync(viewerId, author.Id);
            return _mapper.Map<IEnumerable<PostDto>>(posts, ForViewer(viewerId));
        }

        public async Task<LikeDto> ToggleLikeAsync(string userId, int postId)
        {
            if (!await _posts.IsVisibleAsync(userId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var post = await _posts.GetByIdAsync(postId);
            var like = await _postLikes.ToggleAsync(postId, userId);
            if (post is not null)
            {
                if (like.Liked)
                {
                    await _notifications.AddAsync(NotificationType.PostLike, userId, post.AuthorId, postId: postId);
                }
                else
                {
                    await _notifications.RemoveAsync(NotificationType.PostLike, userId, post.AuthorId, postId: postId);
                }
            }

            return like;
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

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
