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
        private readonly ICommentLikeService _commentLikes;
        private readonly IPostRepository _posts;
        private readonly INotificationService _notifications;
        private readonly IAccountRepository _accounts;
        private readonly IMentionService _mentions;
        private readonly IValidator<CreateCommentDto> _validator;
        private readonly IMapper _mapper;

        public CommentService(ICommentRepository comments,
                              ICommentLikeService commentLikes,
                              IPostRepository posts,
                              INotificationService notifications,
                              IAccountRepository accounts,
                              IMentionService mentions,
                              IValidator<CreateCommentDto> validator,
                              IMapper mapper)
        {
            _comments = comments;
            _commentLikes = commentLikes;
            _posts = posts;
            _notifications = notifications;
            _accounts = accounts;
            _mentions = mentions;
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

            Comment? parent = null;
            if (model.ParentCommentId.HasValue)
            {
                parent = await _comments.GetByIdAsync(model.ParentCommentId.Value);
                if (parent is null || parent.PostId != postId)
                {
                    throw new BadRequestException("You can only reply to a comment on this post.");
                }
            }

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = userId,
                Text = model.Text.Trim(),
                ParentCommentId = parent?.Id
            };

            await _comments.AddAsync(comment);
            comment.Author = author;

            var post = await _posts.GetByIdAsync(postId);
            if (parent is not null)
            {
                await _notifications.AddAsync(NotificationType.CommentReply, userId, parent.AuthorId, postId: postId, commentId: comment.Id, snippet: comment.Text);
            }

            if (post is not null && (parent is null || parent.AuthorId != post.AuthorId))
            {
                await _notifications.AddAsync(NotificationType.PostComment, userId, post.AuthorId, postId: postId, commentId: comment.Id, snippet: comment.Text);
            }

            await _mentions.NotifyAsync(NotificationType.CommentMention, userId, comment.Text, postId: postId, commentId: comment.Id);

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

            foreach (var item in await _comments.GetThreadAsync(commentId))
            {
                await _notifications.RemoveForCommentAsync(item.Id);
                await _comments.DeleteAsync(item);
            }
        }

        public async Task<LikeDto> ToggleLikeAsync(string userId, int commentId)
        {
            var comment = await _comments.GetByIdAsync(commentId);
            if (comment is null || !await _posts.IsVisibleAsync(userId, comment.PostId))
            {
                throw new NotFoundException("Comment not found.");
            }

            var like = await _commentLikes.ToggleAsync(commentId, userId);
            if (like.Liked)
            {
                await _notifications.AddAsync(NotificationType.CommentLike, userId, comment.AuthorId, postId: comment.PostId, commentId: commentId, snippet: comment.Text);
            }
            else
            {
                await _notifications.RemoveAsync(NotificationType.CommentLike, userId, comment.AuthorId, postId: comment.PostId, commentId: commentId);
            }

            return like;
        }
    }
}
