using Domain.Constants;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class AccountTests
    {
        [Fact]
        public async Task Login_succeeds_for_a_confirmed_member()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "member@test.discode");

            var login = await host.Get<IAccountService>().LoginAsync(new LoginDto
            {
                Email = user.Email!,
                Password = TestHost.Password
            });

            Assert.True(login.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(login.Token));
            Assert.Contains(Roles.Member, login.Roles);
        }

        [Fact]
        public async Task Login_fails_with_the_wrong_password()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "member@test.discode");

            var login = await host.Get<IAccountService>().LoginAsync(new LoginDto
            {
                Email = user.Email!,
                Password = "Wrong.123!"
            });

            Assert.False(login.IsSuccess);
            Assert.Contains("Invalid email or password.", login.Errors);
            Assert.Null(login.Token);
        }

        [Fact]
        public async Task Login_fails_before_the_email_is_confirmed()
        {
            using var host = new TestHost();
            var accounts = host.Get<IAccountService>();

            var registered = await accounts.RegisterAsync(new RegisterDto
            {
                Email = "new@test.discode",
                DisplayName = "New User",
                UserName = "newuser",
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            });

            Assert.True(registered.IsSuccess);
            Assert.True(registered.RequiresEmailConfirmation);

            var login = await accounts.LoginAsync(new LoginDto
            {
                Email = "new@test.discode",
                Password = TestHost.Password
            });

            Assert.False(login.IsSuccess);
            Assert.Contains(login.Errors, error => error.Contains("Confirm your email"));
        }

        [Fact]
        public async Task Duplicate_email_does_not_create_a_second_account()
        {
            using var host = new TestHost();
            await host.AddUserAsync(Roles.Member, "taken@test.discode");

            var result = await host.Get<IAccountService>().RegisterAsync(new RegisterDto
            {
                Email = "taken@test.discode",
                DisplayName = "Other",
                UserName = "othername",
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("already registered", result.Errors[0]);
        }

        [Fact]
        public async Task Profile_and_settings_can_be_updated()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "profile@test.discode");
            var accounts = host.Get<IAccountService>();

            var profile = await accounts.GetProfileAsync(user.Id);
            Assert.Equal(user.UserName, profile.UserName);
            Assert.True(profile.IsPublic);

            var updated = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto
            {
                DisplayName = "Nova",
                Bio = "hello there",
                Pronouns = "they/them",
                CustomStatus = "coding",
                IsPublic = false
            });

            Assert.True(updated.IsSuccess);
            Assert.Equal("Nova", updated.Profile!.DisplayName);
            Assert.Equal("hello there", updated.Profile.Bio);
            Assert.False(updated.Profile.IsPublic);

            await accounts.UpdateSettingsAsync(user.Id, new UpdateSettingsDto
            {
                Theme = "dark",
                NotifyAll = false
            });

            var after = await accounts.GetProfileAsync(user.Id);
            Assert.Equal("dark", after.Theme);
            Assert.False(after.NotifyAll);
        }

        [Fact]
        public async Task Username_and_password_can_be_changed()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "rename@test.discode");
            var accounts = host.Get<IAccountService>();

            var renamed = await accounts.ChangeUsernameAsync(user.Id, new ChangeUsernameDto
            {
                UserName = "newhandle",
                CurrentPassword = TestHost.Password
            });

            Assert.True(renamed.IsSuccess);
            Assert.Equal("newhandle", renamed.Profile!.UserName);

            var changed = await accounts.ChangePasswordAsync(user.Id, new ChangePasswordDto
            {
                CurrentPassword = TestHost.Password,
                NewPassword = "NewPass.123!",
                ConfirmPassword = "NewPass.123!"
            });

            Assert.True(changed.IsSuccess);

            var login = await accounts.LoginAsync(new LoginDto
            {
                Email = user.Email!,
                Password = "NewPass.123!"
            });

            Assert.True(login.IsSuccess);
        }

        [Fact]
        public async Task Search_finds_another_member_and_opens_their_profile()
        {
            using var host = new TestHost();
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var other = await host.AddUserAsync(Roles.Member, "target@test.discode");
            var accounts = host.Get<IAccountService>();

            var hits = (await accounts.SearchUserAsync("target", viewer.Id)).ToList();
            Assert.Contains(hits, row => row.UserName == other.UserName);
            Assert.DoesNotContain(hits, row => row.Id == viewer.Id);

            var profile = await accounts.GetUserProfileAsync(viewer.Id, other.UserName!);
            Assert.Equal(other.Id, profile.UserId);
            Assert.Equal("none", profile.Relation);
        }

        [Fact]
        public async Task Forgot_password_sends_a_reset_link_that_works()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "reset@test.discode");
            var accounts = host.Get<IAccountService>();

            var sent = await accounts.ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email! });
            Assert.True(sent.IsSuccess);
            Assert.Contains(host.Mail.Sent, mail => mail.To == user.Email);

            var body = host.Mail.Sent.Last(mail => mail.To == user.Email).Body;
            var tokenPart = body.Split("token=", 2)[1];
            var token = Uri.UnescapeDataString(tokenPart.Split('&')[0]);

            var reset = await accounts.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = user.Email!,
                Token = token,
                NewPassword = "Reset.123!",
                ConfirmPassword = "Reset.123!"
            });

            Assert.True(reset.IsSuccess);

            var login = await accounts.LoginAsync(new LoginDto
            {
                Email = user.Email!,
                Password = "Reset.123!"
            });

            Assert.True(login.IsSuccess);
        }

        [Fact]
        public async Task Resend_code_is_refused_once_the_email_is_confirmed()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "confirmed@test.discode");

            var result = await host.Get<IAccountService>().ResendCodeAsync(new ResendCodeDto { Email = user.Email! });

            Assert.False(result.IsSuccess);
            Assert.Contains("already confirmed", result.Errors[0]);
        }

        [Fact]
        public async Task Forgot_password_does_not_reveal_a_missing_account()
        {
            using var host = new TestHost();

            var result = await host.Get<IAccountService>().ForgotPasswordAsync(new ForgotPasswordDto
            {
                Email = "nobody@test.discode"
            });

            Assert.True(result.IsSuccess);
            Assert.Empty(host.Mail.Sent);
        }

        [Fact]
        public async Task The_wrong_current_password_does_not_rename_the_account()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "rename@test.discode");

            var result = await host.Get<IAccountService>().ChangeUsernameAsync(user.Id, new ChangeUsernameDto
            {
                UserName = "stolen",
                CurrentPassword = "Wrong.123!"
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("incorrect", result.Errors[0]);
        }

        [Fact]
        public async Task An_unknown_theme_is_rejected()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "theme@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IAccountService>().UpdateSettingsAsync(user.Id, new UpdateSettingsDto { Theme = "neon" }));

            Assert.Contains("light or dark", error.Message);
        }
    }
}
