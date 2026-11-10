using Domain.Constants;
using Domain.Enums;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.ServerMessages;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerMessageTests
    {
        [Fact]
        public async Task Members_can_send_edit_pin_and_react_in_a_text_channel()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);
            var textId = await TextChannelId(host, owner.Id, created.Server.Id);
            var messages = host.Get<IServerMessageService>();

            var sent = await messages.SendAsync(owner.Id, created.Server.Id, textId, new SendServerMessageDto { Text = "hello server" });
            Assert.Equal("hello server", sent.Message.Text);
            Assert.Contains(owner.Id, sent.ViewerIds);

            var edited = await messages.EditAsync(owner.Id, created.Server.Id, textId, sent.Message.Id, new EditServerMessageDto { Text = "hello everyone" });
            Assert.Equal("hello everyone", edited.Views[owner.Id].Text);

            var pinned = await messages.PinAsync(owner.Id, created.Server.Id, textId, sent.Message.Id, new PinServerMessageDto { Pinned = true });
            Assert.NotNull(pinned.Views[owner.Id].PinnedAt);
            Assert.Single(await messages.GetPinsAsync(owner.Id, created.Server.Id, textId));

            var reacted = await messages.ToggleReactionAsync(owner.Id, created.Server.Id, textId, sent.Message.Id, new ToggleReactionDto { Emoji = "🎉" });
            Assert.Contains(reacted.Views[owner.Id].Reactions, row => row.Emoji == "🎉");

            Assert.Contains(await messages.GetMessagesAsync(owner.Id, created.Server.Id, textId, null), row => row.Id == sent.Message.Id);
        }

        [Fact]
        public async Task Only_the_owner_or_manage_messages_roles_can_pin()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var textId = await TextChannelId(host, owner.Id, created.Server.Id);
            var messages = host.Get<IServerMessageService>();
            var sent = await messages.SendAsync(owner.Id, created.Server.Id, textId, new SendServerMessageDto { Text = "pin me" });

            var denied = await Assert.ThrowsAsync<ForbiddenException>(() =>
                messages.PinAsync(member.Id, created.Server.Id, textId, sent.Message.Id, new PinServerMessageDto { Pinned = true }));
            Assert.Contains("Manage Messages", denied.Message);

            var ownerPin = await messages.PinAsync(owner.Id, created.Server.Id, textId, sent.Message.Id, new PinServerMessageDto { Pinned = true });
            Assert.NotNull(ownerPin.Views[owner.Id].PinnedAt);

            var roles = host.Get<IServerRoleService>();
            var everyone = (await roles.GetRolesAsync(owner.Id, created.Server.Id)).First(row => row.IsEveryone);
            await roles.UpdateAsync(owner.Id, created.Server.Id, everyone.Id, new UpdateServerRoleDto
            {
                Name = everyone.Name,
                Color = everyone.Color,
                Permissions = everyone.Permissions | (long)ServerPermission.ManageMessages,
                DisplaySeparately = everyone.DisplaySeparately
            });

            var memberPin = await messages.PinAsync(member.Id, created.Server.Id, textId, sent.Message.Id, new PinServerMessageDto { Pinned = false });
            Assert.Null(memberPin.Views[member.Id].PinnedAt);
        }

        [Fact]
        public async Task A_server_poll_can_be_voted_on()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);
            var textId = await TextChannelId(host, owner.Id, created.Server.Id);
            var messages = host.Get<IServerMessageService>();

            var poll = await messages.SendPollAsync(owner.Id, created.Server.Id, textId, TestHost.Poll("Raid tonight?"));
            Assert.True(poll.Message.IsPoll);
            var optionId = poll.Message.Poll!.Options[1].Id;

            var voted = await messages.VotePollAsync(owner.Id, created.Server.Id, textId, poll.Message.Id, new VotePollDto { OptionIds = { optionId } });
            Assert.True(voted.Views[owner.Id].Poll!.Voted);
        }

        private static async Task<int> TextChannelId(TestHost host, string userId, int serverId)
        {
            var channels = await host.Get<IServerChannelService>().GetAsync(userId, serverId);
            return channels.Categories.SelectMany(category => category.Channels).First(row => row.Kind == "text").Id;
        }
    }
}
