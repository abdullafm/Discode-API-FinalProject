using Domain.Constants;
using Service.Helpers.DTOs.ServerVoice;
using Service.Helpers.DTOs.Servers;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerVoiceTests
    {
        [Fact]
        public async Task A_member_can_join_mute_and_leave_voice()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(guest.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var voiceId = await VoiceChannelId(host, owner.Id, created.Server.Id);
            var voice = host.Get<IServerVoiceService>();

            var joined = await voice.JoinAsync(owner.Id, created.Server.Id, voiceId, new JoinServerVoiceDto());
            Assert.Contains(created.Server.Id, joined.ServerIds);

            await voice.JoinAsync(guest.Id, created.Server.Id, voiceId, new JoinServerVoiceDto());
            Assert.True(await voice.AreTogetherAsync(owner.Id, guest.Id));

            var muted = await voice.UpdateAsync(owner.Id, new UpdateServerVoiceDto { IsMuted = true });
            Assert.Contains(created.Server.Id, muted.ServerIds);

            var snapshot = await voice.GetAsync(owner.Id, created.Server.Id);
            Assert.NotNull(snapshot.Current);
            Assert.True(snapshot.Current!.IsMuted);
            Assert.Contains(snapshot.Channels.SelectMany(channel => channel.Users), row => row.UserId == guest.Id);

            var left = await voice.LeaveAsync(guest.Id);
            Assert.Contains(created.Server.Id, left.ServerIds);
            Assert.False(await voice.AreTogetherAsync(owner.Id, guest.Id));
        }

        [Fact]
        public async Task The_owner_can_server_mute_another_member()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(guest.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var voiceId = await VoiceChannelId(host, owner.Id, created.Server.Id);
            var voice = host.Get<IServerVoiceService>();

            await voice.JoinAsync(guest.Id, created.Server.Id, voiceId, new JoinServerVoiceDto());
            var moderated = await voice.ModerateAsync(owner.Id, created.Server.Id, guest.Id, new ModerateServerVoiceDto { ServerMuted = true });

            Assert.Equal("muted", moderated.Kind);
            Assert.Equal(guest.Id, moderated.TargetUserId);
        }

        private static async Task<int> VoiceChannelId(TestHost host, string userId, int serverId)
        {
            var channels = await host.Get<IServerChannelService>().GetAsync(userId, serverId);
            return channels.Categories.SelectMany(category => category.Channels).First(row => row.Kind == "voice").Id;
        }
    }
}
