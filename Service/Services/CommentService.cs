using AutoMapper;
using Domain.Entities;
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
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateCommentDto> _validator;
        private readonly IMapper _mapper;

        public CommentService(ICommentRepository comments,
                              ICommentLikeRepository commentLikes,
                              IPostRepository posts,
                              IAccountRepository accounts,
                              IValidator<CreateCommentDto> validator,
                              IMapper mapper)
        {
            _comments = comments;
            _commentLikes = commentLikes;
            _posts = posts;
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

            await _comments.CreateAsync(comment);
            comment.Author = author;
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
            }
            else
            {
                await _commentLikes.DeleteAsync(like);
            }

            return new LikeDto
            {
                Liked = like is null,
                Count = await _commentLikes.CountAsync(commentId)
            };
        }
    }
}
