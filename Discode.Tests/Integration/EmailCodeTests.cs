using Microsoft.AspNetCore.Identity;
using Domain.Entities;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class EmailCodeTests
    {
        [Fact]
        public async Task The_verification_code_confirms_the_account()
        {
            using var host = new TestHost();
            var accounts = host.Get<IAccountService>();

            var registered = await accounts.RegisterAsync(new RegisterDto
            {
                Email = "verify@test.discode",
                DisplayName = "Verify",
                UserName = "verifyme",
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            });

            Assert.True(registered.IsSuccess);
            Assert.Single(host.Mail.Sent);

            var user = await host.Get<UserManager<AppUser>>().FindByEmailAsync("verify@test.discode");
            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
            Assert.Equal(4, user.EmailVerificationCode?.Length);

            var verified = await accounts.VerifyEmailAsync(new VerifyEmailDto
            {
                Email = "verify@test.discode",
                Code = user.EmailVerificationCode!
            });

            Assert.True(verified.IsSuccess);

            var login = await accounts.LoginAsync(new LoginDto
            {
                Email = "verify@test.discode",
                Password = TestHost.Password
            });

            Assert.True(login.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(login.Token));
        }

        [Fact]
        public async Task A_wrong_code_does_not_confirm_the_account()
        {
            using var host = new TestHost();
            var accounts = host.Get<IAccountService>();

            await accounts.RegisterAsync(new RegisterDto
            {
                Email = "wrong@test.discode",
                DisplayName = "Wrong",
                UserName = "wrongcode",
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            });

            var user = await host.Get<UserManager<AppUser>>().FindByEmailAsync("wrong@test.discode");
            var other = user!.EmailVerificationCode == "0000" ? "1111" : "0000";

            var result = await accounts.VerifyEmailAsync(new VerifyEmailDto
            {
                Email = "wrong@test.discode",
                Code = other
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("incorrect", result.Errors[0]);
        }
    }
}
