using Domain.Constants;
using Service.Helpers.DTOs.Posts;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class CommentTests
    {
        [Fact]
        public async Task A_comment_can_be_created_liked_and_edited()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var post = await host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto { Text = "talk about this" });
            var comments = host.Get<ICommentService>();

            var created = await comments.CreateAsync(viewer.Id, post.Id, new CreateCommentDto { Text = "nice post" });
            Assert.Equal("nice post", created.Text);
            Assert.Equal(post.Id, created.PostId);

            var liked = await comments.ToggleLikeAsync(author.Id, created.Id);
            Assert.True(liked.Liked);
            Assert.Equal(1, liked.Count);

            var edited = await comments.UpdateAsync(viewer.Id, created.Id, new UpdateTextDto { Text = "edited take" });
            Assert.Equal("edited take", edited.Text);
            Assert.NotNull(edited.EditedAt);
        }

        [Fact]
        public async Task A_reply_keeps_the_parent_comment()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var post = await host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto { Text = "thread" });
            var comments = host.Get<ICommentService>();

            var parent = await comments.CreateAsync(viewer.Id, post.Id, new CreateCommentDto { Text = "first" });
            var reply = await comments.CreateAsync(author.Id, post.Id, new CreateCommentDto
            {
                Text = "second",
                ParentCommentId = parent.Id
            });

            Assert.Equal(parent.Id, reply.ParentCommentId);
        }
    }
}
