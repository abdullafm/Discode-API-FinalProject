using Domain.Constants;
using Service.Helpers.DTOs.Live;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class LivePresenceTests
    {
        [Fact]
        public async Task A_host_can_go_live_and_a_viewer_can_join()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            await host.MakeFriendsAsync(streamer, viewer);
            var live = host.Get<ILiveStreamService>();

            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto
            {
                Title = "Friday raid",
                Category = "Gaming"
            });
            Assert.True(started.IsMine);
            Assert.Equal("Friday raid", started.Title);

            await live.SetThumbnailAsync(streamer.Id, new LiveThumbnailDto { ImageUrl = "/uploads/live/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg" });
            var invited = await live.InviteAsync(streamer.Id, started.Id, new[] { viewer.UserName! });
            Assert.Contains(viewer.UserName, invited.UserNames);

            var join = await live.JoinAsync(viewer.Id, "viewer-conn", started.Id);
            Assert.Equal("host-conn", join.HostConnectionId);
            Assert.Equal(started.Id, join.Stream.Id);

            var chat = await live.CreateMessageAsync(viewer.Id, "viewer-conn", started.Id, "hello host");
            Assert.Equal("hello host", chat.Text);

            Assert.True(await live.CanSignalAsync("host-conn", started.Id, "viewer-conn"));
            Assert.Contains(await live.GetViewersAsync("host-conn", started.Id), row => row.UserName == viewer.UserName);
            Assert.Contains(await live.GetActiveAsync(viewer.Id), row => row.Id == started.Id);
            Assert.Contains(await live.GetActiveForAdminAsync(), row => row.Id == started.Id);

            var guest = await live.RequestGuestAsync("viewer-conn", started.Id);
            Assert.Equal("host-conn", guest.HostConnectionId);
            var accepted = await live.RespondGuestAsync("host-conn", started.Id, "viewer-conn", true);
            Assert.Equal(viewer.UserName, accepted!.UserName);

            await live.LeaveAsync("viewer-conn", started.Id);
            var stopped = (await live.StopAsync(streamer.Id)).ToList();
            Assert.Contains(started.Id, stopped);
        }

        [Fact]
        public async Task The_host_can_invite_a_viewer_to_be_a_guest_and_the_viewer_answers()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var other = await host.AddUserAsync(Roles.Member, "other@test.discode");
            var live = host.Get<ILiveStreamService>();
            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto { Title = "Friday raid", Category = "Gaming" });
            await live.JoinAsync(viewer.Id, "viewer-conn", started.Id);
            await live.JoinAsync(other.Id, "other-conn", started.Id);

            // only the host can invite, and only someone who is watching
            await Assert.ThrowsAsync<NotFoundException>(() => live.InviteGuestAsync("viewer-conn", started.Id, other.UserName!));
            await Assert.ThrowsAsync<BadRequestException>(() => live.InviteGuestAsync("host-conn", started.Id, "nobody-here"));
            // an answer without an invitation is refused
            await Assert.ThrowsAsync<BadRequestException>(() => live.AnswerGuestInviteAsync("viewer-conn", started.Id, true));

            var invite = await live.InviteGuestAsync("host-conn", started.Id, viewer.UserName!);
            Assert.Equal("viewer-conn", invite.TargetConnectionId);
            Assert.Equal("Friday raid", invite.Title);

            var declined = await live.AnswerGuestInviteAsync("viewer-conn", started.Id, false);
            Assert.Null(declined.Guest);
            Assert.Equal("host-conn", declined.HostConnectionId);
            // one answer per invitation
            await Assert.ThrowsAsync<BadRequestException>(() => live.AnswerGuestInviteAsync("viewer-conn", started.Id, true));

            await live.InviteGuestAsync("host-conn", started.Id, viewer.UserName!);
            var accepted = await live.AnswerGuestInviteAsync("viewer-conn", started.Id, true);
            Assert.Equal(viewer.UserName, accepted.Guest!.UserName);

            // there is room for one guest only
            var busy = await Assert.ThrowsAsync<BadRequestException>(() => live.InviteGuestAsync("host-conn", started.Id, other.UserName!));
            Assert.Contains("already a guest", busy.Message);
        }

        [Fact]
        public async Task The_host_picks_moderators_and_they_mute_people_who_can_then_only_watch()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var modUser = await host.AddUserAsync(Roles.Member, "mod@test.discode");
            var otherMod = await host.AddUserAsync(Roles.Member, "mod2@test.discode");
            var rude = await host.AddUserAsync(Roles.Member, "rude@test.discode");
            var live = host.Get<ILiveStreamService>();
            var moderation = host.Get<ILiveModerationService>();
            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto { Title = "Friday raid", Category = "Gaming" });
            foreach (var (user, connection) in new[] { (modUser, "mod-conn"), (otherMod, "mod2-conn"), (rude, "rude-conn") })
            {
                await live.JoinAsync(user.Id, connection, started.Id);
            }

            // only the host picks moderators, and only from the people who are watching
            await Assert.ThrowsAsync<ForbiddenException>(() => moderation.SetModeratorAsync("rude-conn", rude.Id, started.Id, modUser.UserName!, true));
            await Assert.ThrowsAsync<BadRequestException>(() => moderation.SetModeratorAsync("host-conn", streamer.Id, started.Id, streamer.UserName!, true));
            var picked = await moderation.SetModeratorAsync("host-conn", streamer.Id, started.Id, modUser.UserName!, true);
            Assert.Contains("mod-conn", picked.TargetConnectionIds);
            await moderation.SetModeratorAsync("host-conn", streamer.Id, started.Id, otherMod.UserName!, true);

            // somebody who is not a moderator can not mute
            await Assert.ThrowsAsync<ForbiddenException>(() => moderation.MuteAsync("rude-conn", rude.Id, started.Id, otherMod.UserName!, 10));

            // chat works until the mute
            await live.CreateMessageAsync(rude.Id, "rude-conn", started.Id, "hello");

            // a moderator mutes for ten minutes; the host can not be muted, nor can a moderator mute a moderator
            await Assert.ThrowsAsync<BadRequestException>(() => moderation.MuteAsync("mod-conn", modUser.Id, started.Id, streamer.UserName!, 10));
            await Assert.ThrowsAsync<ForbiddenException>(() => moderation.MuteAsync("mod-conn", modUser.Id, started.Id, otherMod.UserName!, 10));
            await Assert.ThrowsAsync<BadRequestException>(() => moderation.MuteAsync("mod-conn", modUser.Id, started.Id, rude.UserName!, 0 - 5));
            var muted = await moderation.MuteAsync("mod-conn", modUser.Id, started.Id, rude.UserName!, 10);
            Assert.True(muted.Muted);
            Assert.NotNull(muted.MutedUntil);
            Assert.Contains("rude-conn", muted.TargetConnectionIds);

            // they can only watch: no chat, no asking to join, no being invited
            var chat = await Assert.ThrowsAsync<BadRequestException>(() => live.CreateMessageAsync(rude.Id, "rude-conn", started.Id, "again"));
            Assert.Contains("muted", chat.Message);
            await Assert.ThrowsAsync<BadRequestException>(() => live.RequestGuestAsync("rude-conn", started.Id));
            await Assert.ThrowsAsync<BadRequestException>(() => live.InviteGuestAsync("host-conn", started.Id, rude.UserName!));

            // the host and the moderators see the list, others do not
            var list = await moderation.GetMutesAsync("mod2-conn", otherMod.Id, started.Id);
            Assert.Equal(rude.UserName, Assert.Single(list).UserName);
            await Assert.ThrowsAsync<ForbiddenException>(() => moderation.GetMutesAsync("rude-conn", rude.Id, started.Id));
            var rows = (await live.GetViewersAsync("host-conn", started.Id)).ToList();
            Assert.True(rows.Single(x => x.UserName == rude.UserName).IsMuted);
            Assert.True(rows.Single(x => x.UserName == modUser.UserName).IsModerator);
            var seenByViewer = (await live.GetViewersAsync("rude-conn", started.Id)).ToList();
            Assert.All(seenByViewer, x => Assert.False(x.IsMuted || x.IsModerator));

            // it belongs to the host, so it is still there in the next stream, until it is taken off
            await live.StopAsync(streamer.Id);
            var next = await live.StartAsync(streamer.Id, "host-conn-2", new StartLiveDto { Title = "Saturday", Category = "Gaming" });
            var again = await live.JoinAsync(rude.Id, "rude-conn-2", next.Id);
            Assert.NotNull(again.Mute);
            Assert.False(again.IsModerator);
            await live.JoinAsync(modUser.Id, "mod-conn-2", next.Id);
            Assert.True((await live.JoinAsync(otherMod.Id, "mod2-conn-2", next.Id)).IsModerator);

            var unmuted = await moderation.UnmuteAsync("mod-conn-2", modUser.Id, next.Id, rude.UserName!);
            Assert.False(unmuted.Muted);
            Assert.Contains("rude-conn-2", unmuted.TargetConnectionIds);
            await live.CreateMessageAsync(rude.Id, "rude-conn-2", next.Id, "sorry");
            await Assert.ThrowsAsync<BadRequestException>(() => moderation.UnmuteAsync("mod-conn-2", modUser.Id, next.Id, rude.UserName!));

            // "for good" has no end time and a guest who is muted is taken off the stream
            await live.RequestGuestAsync("rude-conn-2", next.Id);
            await live.RespondGuestAsync("host-conn-2", next.Id, "rude-conn-2", true);
            var forever = await moderation.MuteAsync("host-conn-2", streamer.Id, next.Id, rude.UserName!, 0);
            Assert.Null(forever.MutedUntil);
            Assert.Equal("rude-conn-2", forever.EndedGuestConnectionId);

            // taking a moderator away
            await moderation.SetModeratorAsync("host-conn-2", streamer.Id, next.Id, otherMod.UserName!, false);
            await Assert.ThrowsAsync<ForbiddenException>(() => moderation.MuteAsync("mod2-conn-2", otherMod.Id, next.Id, modUser.UserName!, 5));
        }

        [Fact]
        public async Task Somebody_who_blocked_or_was_blocked_does_not_see_the_stream_and_is_sent_away()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            var fan = await host.AddUserAsync(Roles.Member, "fan@test.discode");
            var live = host.Get<ILiveStreamService>();
            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto { Title = "Friday raid", Category = "Gaming" });
            await live.JoinAsync(viewer.Id, "viewer-conn", started.Id);
            await live.JoinAsync(fan.Id, "fan-conn", started.Id);
            Assert.Contains(await live.GetActiveAsync(viewer.Id), row => row.Id == started.Id);

            // the host blocks the viewer: the viewer is sent away and the stream is gone from the list
            await host.Get<IUserBlockService>().BlockAsync(streamer.Id, new Service.Helpers.DTOs.Friends.BlockUserDto { UserName = viewer.UserName });
            var left = await live.EjectBlockedAsync(streamer.Id, viewer.Id);
            Assert.Equal("viewer-conn", Assert.Single(left).ConnectionId);
            Assert.DoesNotContain(await live.GetActiveAsync(viewer.Id), row => row.Id == started.Id);
            await Assert.ThrowsAsync<NotFoundException>(() => live.JoinAsync(viewer.Id, "viewer-conn-2", started.Id));
            Assert.Contains(await live.GetActiveAsync(fan.Id), row => row.Id == started.Id);

            // the other way round: a viewer blocks the host and does not see the stream either
            await host.Get<IUserBlockService>().BlockAsync(fan.Id, new Service.Helpers.DTOs.Friends.BlockUserDto { UserName = streamer.UserName });
            Assert.Equal("fan-conn", Assert.Single(await live.EjectBlockedAsync(fan.Id, streamer.Id)).ConnectionId);
            Assert.DoesNotContain(await live.GetActiveAsync(fan.Id), row => row.Id == started.Id);
        }

        [Fact]
        public async Task Going_live_requires_a_known_category()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<ILiveStreamService>().StartAsync(streamer.Id, "host-conn", new StartLiveDto
                {
                    Title = "Untitled",
                    Category = "Cooking"
                }));

            Assert.Contains("category", error.Message);
        }

        [Fact]
        public async Task Presence_connects_and_lists_friends()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "pal@test.discode");
            await host.MakeFriendsAsync(user, friend);

            var connected = await host.Get<IPresenceTrackerService>().ConnectAsync(user.Id, "conn-1");
            Assert.Equal(user.Id, connected.UserId);
            Assert.Equal("online", connected.PreferredStatus);

            var change = await host.Get<IPresenceService>().GetChangeAsync(user.Id, false);
            Assert.Equal("online", change.Status);
            Assert.Contains(friend.Id, await host.Get<IPresenceService>().GetFriendIdsAsync(user.Id));
        }

        [Fact]
        public async Task An_unknown_presence_status_is_rejected()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IPresenceService>().SetStatusAsync(user.Id, "busy"));

            Assert.Contains("valid status", error.Message);
        }

        [Fact]
        public async Task The_host_cannot_join_their_own_stream()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var live = host.Get<ILiveStreamService>();
            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto
            {
                Title = "Solo",
                Category = "Music"
            });

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                live.JoinAsync(streamer.Id, "other-conn", started.Id));

            Assert.Contains("host", error.Message);
        }

        [Fact]
        public async Task Disconnecting_the_host_ends_the_stream()
        {
            using var host = new TestHost();
            var streamer = await host.AddUserAsync(Roles.Member, "host@test.discode");
            var live = host.Get<ILiveStreamService>();
            var started = await live.StartAsync(streamer.Id, "host-conn", new StartLiveDto
            {
                Title = "Bye",
                Category = "Music"
            });

            var ended = await live.DisconnectAsync("host-conn");
            Assert.Contains(started.Id, ended.EndedStreamIds);
            Assert.DoesNotContain(await live.GetActiveAsync(streamer.Id), row => row.Id == started.Id);
        }

        [Fact]
        public async Task Shared_space_ids_include_another_server_member()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(guest.Id, new JoinServerDto { Invite = created.Server.InviteCode });

            var spaces = (await host.Get<IPresenceService>().GetSharedSpaceIdsAsync(owner.Id)).ToList();
            Assert.Contains(guest.Id, spaces);
        }
    }
}
