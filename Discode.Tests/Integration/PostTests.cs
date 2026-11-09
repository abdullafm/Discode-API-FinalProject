using Domain.Constants;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class PostTests
    {
        [Fact]
        public async Task A_text_post_can_be_liked_reposted_edited_and_deleted()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var posts = host.Get<IPostService>();

            var created = await posts.CreateAsync(author.Id, new CreatePostDto { Text = "hello world" });
            Assert.Equal("hello world", created.Text);
            Assert.Equal(author.UserName, created.AuthorUserName);

            var liked = await posts.ToggleLikeAsync(viewer.Id, created.Id);
            Assert.True(liked.Liked);
            Assert.Equal(1, liked.Count);

            var unliked = await posts.ToggleLikeAsync(viewer.Id, created.Id);
            Assert.False(unliked.Liked);

            var reposted = await posts.ToggleRepostAsync(viewer.Id, created.Id);
            Assert.True(reposted.Reposted);
            Assert.Single(await posts.GetRepostsAsync(viewer.Id, viewer.UserName!));

            var edited = await posts.UpdateAsync(author.Id, created.Id, new UpdateTextDto { Text = "updated" });
            Assert.Equal("updated", edited.Text);
            Assert.NotNull(edited.EditedAt);

            Assert.Contains(await posts.GetExploreAsync(viewer.Id), row => row.Id == created.Id);
            Assert.Contains(await posts.GetByUserAsync(viewer.Id, author.UserName!), row => row.Id == created.Id);

            await posts.DeleteAsync(author.Id, created.Id);
            Assert.DoesNotContain(await posts.GetByUserAsync(author.Id, author.UserName!), row => row.Id == created.Id);
        }

        [Fact]
        public async Task A_post_can_be_shared_to_friends_and_lands_in_their_direct_messages()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "friend@test.discode");
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            await host.MakeFriendsAsync(author, friend);
            var posts = host.Get<IPostService>();
            var created = await posts.CreateAsync(author.Id, new CreatePostDto { Text = "look at this" });

            // a person can read one post by its number, and shares it with a note
            Assert.Equal("look at this", (await posts.GetAsync(friend.Id, created.Id)).Text);
            var sent = (await posts.ShareAsync(friend.Id, created.Id, new SharePostDto { UserNames = new List<string> { author.UserName! }, Note = "you have to see this" })).ToList();
            Assert.Equal(2, sent.Count);
            Assert.Contains("/Social/Index?post=" + created.Id, sent[0].MessageForRecipient.Text);
            Assert.Equal("you have to see this", sent[1].MessageForRecipient.Text);
            var chat = await host.Get<IDirectMessageService>().GetChatAsync(author.Id, friend.UserName!);
            Assert.Contains(chat.Messages, m => m.Text.Contains("/Social/Index?post=" + created.Id));

            // nothing to share without a friend, a made-up post is refused, a stranger is not a friend
            await Assert.ThrowsAsync<BadRequestException>(() => posts.ShareAsync(friend.Id, created.Id, new SharePostDto()));
            await Assert.ThrowsAsync<NotFoundException>(() => posts.ShareAsync(friend.Id, created.Id + 999, new SharePostDto { UserNames = new List<string> { author.UserName! } }));
            await Assert.ThrowsAsync<NotFoundException>(() => posts.ShareAsync(friend.Id, created.Id, new SharePostDto { UserNames = new List<string> { stranger.UserName! } }));
        }

        [Fact]
        public async Task Friends_feed_shows_a_friend_post()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "friend@test.discode");
            await host.MakeFriendsAsync(author, friend);

            var created = await host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto { Text = "for friends" });
            var feed = (await host.Get<IPostService>().GetFriendsAsync(friend.Id)).ToList();

            Assert.Contains(feed, row => row.Id == created.Id);
        }

        [Fact]
        public async Task An_empty_post_is_rejected()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto()));

            Assert.Contains("Write something", error.Message);
        }

        [Fact]
        public async Task An_external_image_is_rejected()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto
                {
                    Text = "pic",
                    ImageUrl = "https://cdn.example/x.jpg"
                }));

            Assert.Contains("not valid", error.Message);
        }
    }
}
