using Domain.Constants;
using Service.Helpers.DTOs.Groups;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class GroupCallTests
    {
        [Fact]
        public async Task A_group_member_can_start_a_call()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);

            var group = await host.Get<IGroupService>().CreateAsync(owner.Id, new CreateGroupDto
            {
                Name = "Voice",
                UserNames = { first.UserName!, second.UserName! }
            });

            var started = await host.Get<IGroupCallService>().StartAsync(owner.Id, group.Id, false);
            Assert.True(started.Started);
            Assert.True(started.Call.Active);
            Assert.Equal(group.Id, started.Call.GroupId);

            var mine = await host.Get<IGroupCallService>().GetMineAsync(owner.Id);
            Assert.NotNull(mine);
            Assert.Equal(started.Call.CallId, mine!.CallId);

            var active = await host.Get<IGroupCallService>().GetActiveForGroupAsync(group.Id);
            Assert.NotNull(active);
        }
    }
}
