using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.Settings;
using Service.Helpers.DTOs.DirectMessages;
using Microsoft.Extensions.Options;
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
        private readonly IPostRepostRepository _reposts;
        private readonly INotificationService _notifications;
        private readonly IAccountRepository _accounts;
        private readonly IMentionService _mentions;
        private readonly IDirectMessageService _directMessages;
        private readonly IGroupShareService _groupShare;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly AppSettings _app;
        private readonly IValidator<CreatePostDto> _validator;
        private readonly IMapper _mapper;

        public PostService(IPostRepository posts,
                           IPostLikeService postLikes,
                           IPostRepostRepository reposts,
                           INotificationService notifications,
                           IAccountRepository accounts,
                           IMentionService mentions,
                           IDirectMessageService directMessages,
                           IGroupShareService groupShare,
                           IFriendRequestRepository friendRequests,
                           IOptions<AppSettings> app,
                           IValidator<CreatePostDto> validator,
                           IMapper mapper)
        {
            _posts = posts;
            _postLikes = postLikes;
            _reposts = reposts;
            _notifications = notifications;
            _accounts = accounts;
            _mentions = mentions;
            _directMessages = directMessages;
            _groupShare = groupShare;
            _friendRequests = friendRequests;
            _app = app.Value;
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

        public async Task<PostDto> GetAsync(string viewerId, int postId)
        {
            if (!await _posts.IsVisibleAsync(viewerId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var post = await _posts.GetWithDetailsAsync(postId) ?? throw new NotFoundException("Post not found.");
            return _mapper.Map<PostDto>(post, ForViewer(viewerId));
        }

        public async Task<List<Service.Helpers.DTOs.Groups.GroupChangeDto>> ShareToGroupsAsync(string userId, int postId, SharePostDto model)
        {
            if (model?.GroupIds is null || model.GroupIds.Count == 0)
            {
                return new List<Service.Helpers.DTOs.Groups.GroupChangeDto>();
            }

            if (!await _posts.IsVisibleAsync(userId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var note = (model.Note ?? string.Empty).Trim();
            if (note.Length > 500)
            {
                throw new BadRequestException("The message can be at most 500 characters.");
            }

            var link = _app.ClientUrl.TrimEnd('/') + "/Social/Index?post=" + postId;
            var changes = await _groupShare.SendTextAsync(userId, model.GroupIds, link);
            if (note.Length > 0)
            {
                changes.AddRange(await _groupShare.SendTextAsync(userId, model.GroupIds, note));
            }

            return changes;
        }

        public async Task<IEnumerable<SentMessageDto>> ShareAsync(string userId, int postId, SharePostDto model)
        {
            if (!await _posts.IsVisibleAsync(userId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var names = (model?.UserNames ?? new List<string>()).Select(n => (n ?? string.Empty).Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count == 0)
            {
                if (model?.GroupIds is { Count: > 0 })
                {
                    return new List<SentMessageDto>();
                }

                throw new BadRequestException("Pick at least one friend or group.");
            }

            if (names.Count > 20)
            {
                throw new BadRequestException("Pick at most 20 friends.");
            }

            var note = (model?.Note ?? string.Empty).Trim();
            if (note.Length > 500)
            {
                throw new BadRequestException("The message can be at most 500 characters.");
            }

            var recipients = new List<AppUser>();
            foreach (var name in names)
            {
                var friend = await _accounts.GetByUserNameAsync(name);
                var friendship = friend is null ? null : await _friendRequests.GetBetweenAsync(userId, friend.Id);
                if (friend is null || friend.Id == userId || friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    throw new NotFoundException("You can only share a post with your friends.");
                }

                recipients.Add(friend);
            }

            var link = _app.ClientUrl.TrimEnd('/') + "/Social/Index?post=" + postId;
            var sent = new List<SentMessageDto>();
            foreach (var recipient in recipients)
            {
                var name = recipient.UserName ?? string.Empty;
                sent.Add(await _directMessages.SendAsync(userId, name, new SendMessageDto { Text = link }));
                if (note.Length > 0)
                {
                    sent.Add(await _directMessages.SendAsync(userId, name, new SendMessageDto { Text = note }));
                }
            }

            return sent;
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

        public async Task<RepostDto> ToggleRepostAsync(string userId, int postId)
        {
            if (!await _posts.IsVisibleAsync(userId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var existing = await _reposts.GetByUserAsync(postId, userId);
            if (existing is null)
            {
                await _reposts.AddAsync(new PostRepost { PostId = postId, UserId = userId });
            }
            else
            {
                await _reposts.DeleteAsync(existing);
            }

            return new RepostDto
            {
                Reposted = existing is null,
                Count = await _reposts.CountAsync(postId)
            };
        }

        public async Task<IEnumerable<PostDto>> GetRepostsAsync(string viewerId, string userName)
        {
            var user = await _accounts.GetByUserNameAsync(userName.Trim());
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var posts = await _posts.GetRepostedByAsync(viewerId, user.Id);
            return _mapper.Map<IEnumerable<PostDto>>(posts, ForViewer(viewerId));
        }

        public async Task<PostDto> UpdateAsync(string userId, int postId, UpdateTextDto model)
        {
            var post = await _posts.GetByIdAsync(postId);
            if (post is null || post.AuthorId != userId)
            {
                throw new NotFoundException("Post not found.");
            }

            await _validator.EnsureValidAsync(new CreatePostDto { Text = model.Text, ImageUrl = post.ImageUrl });

            var text = model.Text?.Trim() ?? string.Empty;
            if (!string.Equals(text, post.Text, StringComparison.Ordinal))
            {
                post.Text = text;
                post.EditedAt = DateTime.UtcNow;
                await _posts.UpdateAsync(post);
                await _mentions.NotifyAsync(NotificationType.PostMention, userId, text, postId: post.Id);
            }

            var author = await _accounts.GetByIdAsync(userId);
            post.Author = author!;
            return _mapper.Map<PostDto>(post, ForViewer(userId));
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
