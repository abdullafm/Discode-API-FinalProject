using Domain.Constants;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class IgnoreTests
    {
        [Fact]
        public async Task Ignore_toggles_on_and_off()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");
            var other = await host.AddUserAsync(Roles.Member, "other@test.discode");
            var ignores = host.Get<IUserIgnoreService>();

            var on = await ignores.ToggleAsync(user.Id, new IgnoreUserDto { UserName = other.UserName! });
            Assert.True(on.Ignored);
            Assert.Equal(other.UserName, Assert.Single(await ignores.GetIgnoredUserNamesAsync(user.Id)));

            var off = await ignores.ToggleAsync(user.Id, new IgnoreUserDto { UserName = other.UserName! });
            Assert.False(off.Ignored);
            Assert.Empty(await ignores.GetIgnoredUserNamesAsync(user.Id));
        }

        [Fact]
        public async Task You_cannot_ignore_yourself()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IUserIgnoreService>().ToggleAsync(user.Id, new IgnoreUserDto { UserName = user.UserName! }));

            Assert.Contains("yourself", error.Message);
        }
    }
}
