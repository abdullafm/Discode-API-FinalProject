using Domain.Constants;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ProfileSetupTests
    {
        private static async Task MarkNotDoneAsync(TestHost host, string userId)
        {
            var repo = host.Get<IAccountRepository>();
            var user = (await repo.GetByIdAsync(userId))!;
            user.ProfileSetupDone = false;
            await repo.UpdateAsync(user);
        }

        [Fact]
        public async Task Setup_with_a_photo_saves_it_and_marks_the_setup_done()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "new@test.discode");
            await MarkNotDoneAsync(host, user.Id);
            var accounts = host.Get<IAccountService>();
            Assert.False((await accounts.GetProfileAsync(user.Id)).ProfileSetupDone);

            var result = await accounts.CompleteProfileSetupAsync(user.Id, new CompleteProfileSetupDto { AvatarUrl = "/uploads/avatars/" + user.Id + ".jpg?v=1" });
            Assert.True(result.IsSuccess);
            var profile = await accounts.GetProfileAsync(user.Id);
            Assert.True(profile.ProfileSetupDone);
            Assert.StartsWith("/uploads/avatars/" + user.Id, profile.AvatarUrl);
        }

        [Fact]
        public async Task Setup_without_a_photo_only_marks_it_done()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "new@test.discode");
            await MarkNotDoneAsync(host, user.Id);
            var accounts = host.Get<IAccountService>();

            var result = await accounts.CompleteProfileSetupAsync(user.Id, new CompleteProfileSetupDto());
            Assert.True(result.IsSuccess);
            var profile = await accounts.GetProfileAsync(user.Id);
            Assert.True(profile.ProfileSetupDone);
            Assert.Equal(string.Empty, profile.AvatarUrl);
        }

        [Fact]
        public async Task Setup_refuses_a_picture_that_is_not_the_users_own_upload_and_an_animated_one_without_nitro()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "new@test.discode");
            await MarkNotDoneAsync(host, user.Id);
            var accounts = host.Get<IAccountService>();

            Assert.False((await accounts.CompleteProfileSetupAsync(user.Id, new CompleteProfileSetupDto { AvatarUrl = "https://example.com/x.png" })).IsSuccess);
            Assert.False((await accounts.CompleteProfileSetupAsync(user.Id, new CompleteProfileSetupDto { AvatarUrl = "/uploads/avatars/someone-else.jpg" })).IsSuccess);
            Assert.False((await accounts.CompleteProfileSetupAsync(user.Id, new CompleteProfileSetupDto { AvatarUrl = "/uploads/avatars/" + user.Id + ".gif" })).IsSuccess);
            Assert.False((await accounts.GetProfileAsync(user.Id)).ProfileSetupDone);
        }

        [Fact]
        public void The_letter_picture_uses_the_first_letter_of_the_username()
        {
            Assert.Equal("/avatar/letter/G", LetterAvatar.For("gunel"));
            Assert.Equal("/avatar/letter/7", LetterAvatar.For("7up"));
            Assert.Equal("/avatar/letter/_", LetterAvatar.For("_x"));
            Assert.Equal("/avatar/letter/_", LetterAvatar.For(""));
            Assert.True(LetterAvatar.IsLetter("/avatar/letter/G"));
            Assert.False(LetterAvatar.IsLetter("/uploads/avatars/x.jpg"));
        }

        [Fact]
        public async Task Removing_the_photo_gives_the_letter_picture_back()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "new@test.discode");
            var accounts = host.Get<IAccountService>();

            var result = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "Name", AvatarUrl = "", IsPublic = true });
            Assert.True(result.IsSuccess);
            Assert.Equal(LetterAvatar.For(user.UserName), (await accounts.GetProfileAsync(user.Id)).AvatarUrl);
        }
    }
}
