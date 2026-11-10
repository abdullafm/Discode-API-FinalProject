using Domain.Constants;
using Service.Helpers.DTOs.ServerChannels;
using Service.Helpers.DTOs.ServerMessages;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class SlowModeTests
    {
        [Fact]
        public async Task Slowmode_makes_members_wait_but_not_people_who_manage_messages()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var channels = host.Get<IServerChannelService>();
            var channel = await channels.CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto { Name = "slow", Kind = "text", SlowModeSeconds = 60 });
            Assert.Equal(60, channel.SlowModeSeconds);
            var messages = host.Get<IServerMessageService>();

            await messages.SendAsync(member.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "first" });
            var wait = await Assert.ThrowsAsync<BadRequestException>(() => messages.SendAsync(member.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "second" }));
            Assert.Contains("Slowmode is on", wait.Message);

            await messages.SendAsync(owner.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "owner one" });
            await messages.SendAsync(owner.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "owner two" });
        }

        [Fact]
        public async Task Slowmode_can_be_changed_and_turned_off_and_only_listed_times_are_accepted()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var channels = host.Get<IServerChannelService>();
            var channel = await channels.CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto { Name = "talk", Kind = "text" });
            Assert.Equal(0, channel.SlowModeSeconds);

            await Assert.ThrowsAsync<BadRequestException>(() => channels.UpdateChannelAsync(owner.Id, created.Server.Id, channel.Id, new UpdateServerChannelDto { Name = "talk", SlowModeSeconds = 7 }));
            await Assert.ThrowsAsync<BadRequestException>(() => channels.CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto { Name = "bad", Kind = "text", SlowModeSeconds = 13 }));
            await Assert.ThrowsAsync<ForbiddenException>(() => channels.UpdateChannelAsync(member.Id, created.Server.Id, channel.Id, new UpdateServerChannelDto { Name = "talk", SlowModeSeconds = 30 }));

            var on = await channels.UpdateChannelAsync(owner.Id, created.Server.Id, channel.Id, new UpdateServerChannelDto { Name = "talk", SlowModeSeconds = 300 });
            Assert.Equal(300, on.SlowModeSeconds);
            var messages = host.Get<IServerMessageService>();
            await messages.SendAsync(member.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "a" });
            await Assert.ThrowsAsync<BadRequestException>(() => messages.SendAsync(member.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "b" }));

            var off = await channels.UpdateChannelAsync(owner.Id, created.Server.Id, channel.Id, new UpdateServerChannelDto { Name = "talk", SlowModeSeconds = 0 });
            Assert.Equal(0, off.SlowModeSeconds);
            await messages.SendAsync(member.Id, created.Server.Id, channel.Id, new SendServerMessageDto { Text = "c" });

            var voice = await channels.CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto { Name = "v", Kind = "voice", SlowModeSeconds = 60 });
            Assert.Equal(0, voice.SlowModeSeconds);
        }
    }
}
