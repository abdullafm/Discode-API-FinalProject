using Domain.Constants;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class GroupTests
    {
        [Fact]
        public async Task Friends_can_create_a_group_and_chat_in_it()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);
            var groups = host.Get<IGroupService>();

            var created = await groups.CreateAsync(owner.Id, new CreateGroupDto
            {
                Name = "Crew",
                UserNames = { first.UserName!, second.UserName! }
            });

            Assert.Equal("Crew", created.Name);
            Assert.Equal(3, created.MemberCount);
            Assert.True(created.Members.Single(m => m.UserId == owner.Id).IsOwner);

            var sent = await groups.SendMessageAsync(owner.Id, created.Id, new SendGroupMessageDto { Text = "welcome" });
            Assert.Equal("welcome", sent.Message!.Text);

            var reacted = await groups.ToggleReactionAsync(first.Id, created.Id, sent.Message.Id, new ToggleReactionDto { Emoji = "🔥" });
            Assert.Contains(reacted.Views[first.Id].Reactions, row => row.Emoji == "🔥" && row.Reacted);

            var poll = await groups.SendPollAsync(owner.Id, created.Id, TestHost.Poll("Pizza?"));
            var optionId = poll.Message!.Poll!.Options[0].Id;
            var voted = await groups.VotePollAsync(second.Id, created.Id, poll.Message.Id, new VotePollDto { OptionIds = { optionId } });
            Assert.True(voted.Views[second.Id].Poll!.Voted);

            Assert.Single(await groups.GetMineAsync(first.Id));
            Assert.Contains(await groups.GetMessagesAsync(owner.Id, created.Id), row => row.Text == "welcome");
        }

        [Fact]
        public async Task The_owner_can_rename_add_a_friend_and_transfer_ownership()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            var third = await host.AddUserAsync(Roles.Member, "third@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);
            await host.MakeFriendsAsync(owner, third);
            var groups = host.Get<IGroupService>();

            var created = await groups.CreateAsync(owner.Id, new CreateGroupDto
            {
                Name = "Crew",
                UserNames = { first.UserName!, second.UserName! }
            });

            var renamed = await groups.UpdateAsync(owner.Id, created.Id, new UpdateGroupDto { Name = "Night Crew" });
            Assert.Equal("Night Crew", renamed.Group!.Name);

            var added = await groups.AddMembersAsync(owner.Id, created.Id, new AddGroupMembersDto { UserNames = { third.UserName! } });
            Assert.Equal(4, added.Group!.MemberCount);

            var transferred = await groups.TransferOwnerAsync(owner.Id, created.Id, new TransferGroupOwnerDto { UserName = first.UserName! });
            Assert.Equal(first.UserName, transferred.Group!.OwnerUserName);
        }

        [Fact]
        public async Task A_group_needs_two_friends()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var only = await host.AddUserAsync(Roles.Member, "only@test.discode");
            await host.MakeFriendsAsync(owner, only);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IGroupService>().CreateAsync(owner.Id, new CreateGroupDto
                {
                    Name = "Pair",
                    UserNames = { only.UserName! }
                }));

            Assert.Contains("at least two friends", error.Message);
        }

        [Fact]
        public async Task The_owner_can_open_the_group_again()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);
            var groups = host.Get<IGroupService>();

            var created = await groups.CreateAsync(owner.Id, new CreateGroupDto
            {
                Name = "Crew",
                UserNames = { first.UserName!, second.UserName! }
            });

            var loaded = await groups.GetAsync(owner.Id, created.Id);
            Assert.Equal("Crew", loaded.Name);
            Assert.Equal(3, loaded.MemberCount);
        }

        [Fact]
        public async Task A_stranger_cannot_be_added()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);
            var groups = host.Get<IGroupService>();

            var created = await groups.CreateAsync(owner.Id, new CreateGroupDto
            {
                Name = "Crew",
                UserNames = { first.UserName!, second.UserName! }
            });

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                groups.AddMembersAsync(owner.Id, created.Id, new AddGroupMembersDto { UserNames = { stranger.UserName! } }));

            Assert.Contains("not your friend", error.Message);
        }
    }
}
