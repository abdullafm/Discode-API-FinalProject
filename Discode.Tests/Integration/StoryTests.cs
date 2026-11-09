using Domain.Constants;
using Service.Helpers.DTOs.Stories;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class StoryTests
    {
        [Fact]
        public async Task A_story_can_be_liked_viewed_and_deleted()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            await host.MakeFriendsAsync(author, viewer);
            var stories = host.Get<IStoryService>();

            var created = await stories.CreateAsync(author.Id, new CreateStoryDto { ImageUrl = "/uploads/stories/demo.jpg" });
            Assert.Equal("/uploads/stories/demo.jpg", created.ImageUrl);

            var liked = await stories.ToggleLikeAsync(viewer.Id, created.Id);
            Assert.True(liked.Liked);

            var authorId = await stories.RecordViewAsync(viewer.Id, created.Id);
            Assert.Equal(author.Id, authorId);

            var viewers = (await stories.GetViewersAsync(author.Id, created.Id)).ToList();
            Assert.Contains(viewers, row => row.UserName == viewer.UserName && row.Liked);

            await stories.DeleteAsync(author.Id, created.Id);
            var missing = await Assert.ThrowsAsync<NotFoundException>(() => stories.ToggleLikeAsync(viewer.Id, created.Id));
            Assert.Contains("Story not found", missing.Message);
        }

        [Fact]
        public async Task A_story_needs_an_uploaded_image()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IStoryService>().CreateAsync(author.Id, new CreateStoryDto { ImageUrl = "https://example.com/x.jpg" }));

            Assert.Contains("not valid", error.Message);
        }
    }
}
