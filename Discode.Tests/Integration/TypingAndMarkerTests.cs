using Domain.Constants;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.DTOs.ReadMarkers;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class TypingAndMarkerTests
    {
        // ---------- where I stopped reading

        [Fact]
        public async Task A_chat_never_read_has_no_marker_and_marking_it_remembers_the_time()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "reader@test.discode");
            var markers = host.Get<IChatReadMarkerService>();

            Assert.Null((await markers.GetAsync(user.Id, "dm", "friend")).ReadAt);

            var first = await markers.MarkAsync(user.Id, new MarkReadDto { Kind = "dm", Key = "Friend" });
            Assert.True(first.ReadAt > DateTime.UtcNow.AddMinutes(-1));
            // the name is not case sensitive and marking again moves the time forward without a second row
            var stored = (await markers.GetAsync(user.Id, "DM", "friend")).ReadAt;
            Assert.Equal(first.ReadAt, stored);
            await Task.Delay(20);
            var second = await markers.MarkAsync(user.Id, new MarkReadDto { Kind = "dm", Key = "friend" });
            Assert.True(second.ReadAt > first.ReadAt);
        }

        [Fact]
        public async Task Markers_belong_to_one_person_and_one_chat()
        {
            using var host = new TestHost();
            var one = await host.AddUserAsync(Roles.Member, "one@test.discode");
            var two = await host.AddUserAsync(Roles.Member, "two@test.discode");
            var markers = host.Get<IChatReadMarkerService>();

            await markers.MarkAsync(one.Id, new MarkReadDto { Kind = "group", Key = "5" });

            Assert.Null((await markers.GetAsync(two.Id, "group", "5")).ReadAt);
            Assert.Null((await markers.GetAsync(one.Id, "group", "6")).ReadAt);
            Assert.Null((await markers.GetAsync(one.Id, "channel", "5")).ReadAt);
        }

        [Fact]
        public async Task A_marker_needs_a_known_kind_and_a_sensible_key()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "reader@test.discode");
            var markers = host.Get<IChatReadMarkerService>();

            await Assert.ThrowsAsync<BadRequestException>(() => markers.MarkAsync(user.Id, new MarkReadDto { Kind = "story", Key = "1" }));
            await Assert.ThrowsAsync<BadRequestException>(() => markers.MarkAsync(user.Id, new MarkReadDto { Kind = "dm", Key = " " }));
            await Assert.ThrowsAsync<BadRequestException>(() => markers.MarkAsync(user.Id, new MarkReadDto { Kind = "dm", Key = new string('x', 101) }));
            await Assert.ThrowsAsync<BadRequestException>(() => markers.GetAsync(user.Id, "", "x"));
        }

        // ---------- who is told that somebody types

        [Fact]
        public async Task Typing_in_a_direct_chat_tells_only_the_other_person_and_names_the_chat_after_the_writer()
        {
            using var host = new TestHost();
            var alice = await host.AddUserAsync(Roles.Member, "alice@test.discode");
            var bob = await host.AddUserAsync(Roles.Member, "bob@test.discode");
            var typing = host.Get<ITypingService>();

            var told = await typing.ResolveAsync(alice.Id, "dm", bob.UserName!);
            Assert.NotNull(told);
            Assert.Equal(new[] { bob.Id }, told!.AudienceIds);
            Assert.Equal(alice.UserName!.ToLowerInvariant(), told.Key);
            Assert.Equal("dm", told.Kind);

            Assert.Null(await typing.ResolveAsync(alice.Id, "dm", alice.UserName!));
            Assert.Null(await typing.ResolveAsync(alice.Id, "dm", "nobody-here"));
        }

        [Fact]
        public async Task Typing_to_somebody_who_blocked_you_tells_nobody()
        {
            using var host = new TestHost();
            var alice = await host.AddUserAsync(Roles.Member, "alice@test.discode");
            var bob = await host.AddUserAsync(Roles.Member, "bob@test.discode");
            await host.MakeFriendsAsync(alice, bob);
            await host.Get<IUserBlockService>().BlockAsync(bob.Id, new Service.Helpers.DTOs.Friends.BlockUserDto { UserName = alice.UserName! });

            Assert.Null(await host.Get<ITypingService>().ResolveAsync(alice.Id, "dm", bob.UserName!));
        }

        [Fact]
        public async Task Typing_in_a_group_tells_the_other_members_and_a_stranger_cannot_use_it()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            await host.MakeFriendsAsync(owner, first);
            await host.MakeFriendsAsync(owner, second);
            var group = await host.Get<IGroupService>().CreateAsync(owner.Id, new CreateGroupDto { Name = "Crew", UserNames = { first.UserName!, second.UserName! } });
            var typing = host.Get<ITypingService>();

            var told = await typing.ResolveAsync(first.Id, "group", group.Id.ToString());
            Assert.NotNull(told);
            Assert.Equal(new[] { owner.Id, second.Id }.OrderBy(x => x), told!.AudienceIds.OrderBy(x => x));
            Assert.Equal(group.Id.ToString(), told.Key);

            Assert.Null(await typing.ResolveAsync(stranger.Id, "group", group.Id.ToString()));
            Assert.Null(await typing.ResolveAsync(first.Id, "group", "not-a-number"));
        }

        [Fact]
        public async Task Typing_in_a_server_channel_tells_the_people_who_can_see_it_and_only_if_the_writer_may_write()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var outsider = await host.AddUserAsync(Roles.Member, "outsider@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var channels = await host.Get<IServerChannelService>().GetAsync(owner.Id, created.Server.Id);
            var text = channels.Categories.SelectMany(c => c.Channels).First(c => c.Kind == "text");
            var voice = channels.Categories.SelectMany(c => c.Channels).First(c => c.Kind == "voice");
            var announce = channels.Categories.SelectMany(c => c.Channels).FirstOrDefault(c => c.Kind == "announcement");
            var typing = host.Get<ITypingService>();
            var key = created.Server.Id + ":" + text.Id;

            var told = await typing.ResolveAsync(member.Id, "channel", key);
            Assert.NotNull(told);
            Assert.Contains(owner.Id, told!.AudienceIds);
            Assert.DoesNotContain(member.Id, told.AudienceIds);
            Assert.Equal(key, told.Key);

            Assert.Null(await typing.ResolveAsync(outsider.Id, "channel", key));
            Assert.Null(await typing.ResolveAsync(member.Id, "channel", created.Server.Id + ":" + voice.Id));
            Assert.Null(await typing.ResolveAsync(member.Id, "channel", "garbage"));
            if (announce is not null)
            {
                // a member can not write in an announcement channel, so nobody is told they type there
                Assert.Null(await typing.ResolveAsync(member.Id, "channel", created.Server.Id + ":" + announce.Id));
            }
        }
    }
}
