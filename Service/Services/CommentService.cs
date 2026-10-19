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
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _comments;
        private readonly ICommentLikeRepository _commentLikes;
        private readonly IPostRepository _posts;
        private readonly INotificationService _notifications;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateCommentDto> _validator;
        private readonly IMapper _mapper;

        public CommentService(ICommentRepository comments,
                              ICommentLikeRepository commentLikes,
                              IPostRepository posts,
                              INotificationService notifications,
                              IAccountRepository accounts,
                              IValidator<CreateCommentDto> validator,
                              IMapper mapper)
        {
            _comments = comments;
            _commentLikes = commentLikes;
            _posts = posts;
            _notifications = notifications;
            _accounts = accounts;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<CommentDto> CreateAsync(string userId, int postId, CreateCommentDto model)
        {
            await _validator.EnsureValidAsync(model);

            if (!await _posts.IsVisibleAsync(userId, postId))
            {
                throw new NotFoundException("Post not found.");
            }

            var author = await _accounts.GetByIdAsync(userId);
            if (author is null)
            {
                throw new NotFoundException("User not found.");
            }

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = userId,
                Text = model.Text.Trim()
            };

            await _comments.AddAsync(comment);
            comment.Author = author;

            var post = await _posts.GetByIdAsync(postId);
            if (post is not null)
            {
                await _notifications.AddAsync(NotificationType.PostComment, userId, post.AuthorId, postId: postId, commentId: comment.Id, snippet: comment.Text);
            }

            return _mapper.Map<CommentDto>(comment, options => options.Items[MappingProfile.ViewerIdKey] = userId);
        }

        public async Task DeleteAsync(string userId, int commentId)
        {
            var comment = await _comments.GetByIdAsync(commentId);
            var post = comment is null ? null : await _posts.GetByIdAsync(comment.PostId);
            if (comment is null || post is null || (comment.AuthorId != userId && post.AuthorId != userId))
            {
                throw new NotFoundException("Comment not found.");
            }

            await _comments.DeleteAsync(comment);
            await _notifications.RemoveForCommentAsync(commentId);
        }

        public async Task<LikeDto> ToggleLikeAsync(string userId, int commentId)
        {
            var comment = await _comments.GetByIdAsync(commentId);
            if (comment is null || !await _posts.IsVisibleAsync(userId, comment.PostId))
            {
                throw new NotFoundException("Comment not found.");
            }

            var like = await _commentLikes.GetByUserAsync(commentId, userId);
            if (like is null)
            {
                await _commentLikes.AddAsync(new CommentLike { CommentId = commentId, UserId = userId });
                await _notifications.AddAsync(NotificationType.CommentLike, userId, comment.AuthorId, postId: comment.PostId, commentId: commentId, snippet: comment.Text);
            }
            else
            {
                await _commentLikes.DeleteAsync(like);
                await _notifications.RemoveAsync(NotificationType.CommentLike, userId, comment.AuthorId, postId: comment.PostId, commentId: commentId);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _commentLikes.CountAsync(commentId)
            };
        }
    }
}
