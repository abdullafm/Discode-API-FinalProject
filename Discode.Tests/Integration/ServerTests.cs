using Domain.Constants;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerTests
    {
        [Fact]
        public async Task Creating_a_server_makes_the_user_the_owner()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");

            var created = await host.Get<IServerService>().CreateAsync(owner.Id, new CreateServerDto { Name = "Nexus" });

            Assert.True(created.Joined);
            Assert.Equal("Nexus", created.Server.Name);
            Assert.True(created.Server.IsOwner);
            Assert.False(string.IsNullOrWhiteSpace(created.Server.InviteCode));
            Assert.Contains(owner.Id, created.MemberIds);

            var mine = (await host.Get<IServerService>().GetMineAsync(owner.Id)).ToList();
            Assert.Single(mine);
            Assert.Equal(created.Server.Id, mine[0].Id);
        }

        [Fact]
        public async Task A_member_can_join_with_the_invite_code()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            var servers = host.Get<IServerService>();

            var created = await servers.CreateAsync(owner.Id, new CreateServerDto { Name = "Nexus" });
            var joined = await servers.JoinAsync(guest.Id, new JoinServerDto { Invite = created.Server.InviteCode });

            Assert.True(joined.Joined);
            Assert.Equal(2, joined.Server.MemberCount);
            Assert.False(joined.Server.IsOwner);
        }

        [Fact]
        public async Task A_missing_invite_is_rejected()
        {
            using var host = new TestHost();
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IServerService>().JoinAsync(guest.Id, new JoinServerDto { Invite = "no-such-invite" }));

            Assert.Contains("invalid", error.Message);
        }

        [Fact]
        public async Task The_owner_can_update_profile_invite_and_preferences()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var servers = host.Get<IServerService>();
            var created = await servers.CreateAsync(owner.Id, new CreateServerDto { Name = "Nexus" });

            var profile = await servers.UpdateProfileAsync(owner.Id, created.Server.Id, new UpdateServerProfileDto
            {
                Name = "Nexus Prime",
                Description = "A hangout",
                BannerColor = "blue",
                IsPrivate = true
            });
            Assert.Equal("Nexus Prime", profile.Server.Name);
            Assert.True(profile.Server.IsPrivate);

            // a custom link is a perk of a server with 14 boosts
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                servers.UpdateInviteAsync(owner.Id, created.Server.Id, new UpdateServerInviteDto { InviteCode = "nexus-prime" }));
            var boosts = host.Get<Repository.Repositories.Interfaces.IServerBoostRepository>();
            for (var i = 0; i < Domain.Common.NitroRules.BoostsForPerks; i++)
            {
                await boosts.AddAsync(new Domain.Entities.ServerBoost { ServerId = created.Server.Id, UserId = owner.Id });
            }

            var invite = await servers.UpdateInviteAsync(owner.Id, created.Server.Id, new UpdateServerInviteDto { InviteCode = "nexus-prime" });
            Assert.Equal("nexus-prime", invite.Server.CustomLink);

            var preview = await servers.GetInviteAsync(owner.Id, "nexus-prime");
            Assert.True(preview.IsMember);
            Assert.Equal(created.Server.Id, preview.ServerId);
        }

        [Fact]
        public async Task The_owner_cannot_leave_the_server()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IServerService>().LeaveAsync(owner.Id, created.Server.Id));

            Assert.Contains("owner cannot leave", error.Message);
        }

        [Fact]
        public async Task Members_and_banner_are_available()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            var servers = host.Get<IServerService>();

            var created = await servers.CreateAsync(owner.Id, new CreateServerDto { Name = "Alpha" });
            await servers.JoinAsync(guest.Id, new JoinServerDto { Invite = created.Server.InviteCode });

            var members = (await servers.GetMembersAsync(owner.Id, created.Server.Id)).ToList();
            Assert.Equal(2, members.Count);

            var banner = await servers.UpdateBannerAsync(owner.Id, created.Server.Id, new UpdateServerBannerDto { BannerUrl = string.Empty });
            Assert.Equal(string.Empty, banner.Server.BannerUrl);

            var loaded = await servers.GetAsync(owner.Id, created.Server.Id);
            Assert.True(loaded.IsOwner);
            Assert.Equal(2, loaded.MemberCount);
        }
    }
}
