using Domain.Constants;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class OwnershipTests
    {
        [Fact]
        public async Task The_owner_can_hand_the_server_to_a_member_and_loses_owner_rights()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(owner);
            var servers = host.Get<IServerService>();
            await servers.JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });

            var changed = await servers.TransferOwnerAsync(owner.Id, created.Server.Id, new TransferServerOwnerDto { UserId = member.Id });
            Assert.False(changed.Server.IsOwner);
            Assert.Equal(member.UserName, changed.Server.OwnerUserName);
            Assert.True((await servers.GetAsync(member.Id, created.Server.Id)).IsOwner);

            await Assert.ThrowsAsync<ForbiddenException>(() => servers.TransferOwnerAsync(owner.Id, created.Server.Id, new TransferServerOwnerDto { UserId = owner.Id }));
            await Assert.ThrowsAsync<ForbiddenException>(() => servers.DeleteAsync(owner.Id, created.Server.Id, new DeleteServerDto { Name = created.Server.Name }));
            var mismatch = await Assert.ThrowsAsync<BadRequestException>(() => servers.DeleteAsync(member.Id, created.Server.Id, new DeleteServerDto { Name = "wrong name" }));
            Assert.Contains("doesn't match", mismatch.Message);
        }

        [Fact]
        public async Task Only_a_member_other_than_the_owner_can_become_the_owner()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            var created = await host.CreateServerAsync(owner);
            var servers = host.Get<IServerService>();

            await Assert.ThrowsAsync<BadRequestException>(() => servers.TransferOwnerAsync(owner.Id, created.Server.Id, new TransferServerOwnerDto { UserId = stranger.Id }));
            await Assert.ThrowsAsync<BadRequestException>(() => servers.TransferOwnerAsync(owner.Id, created.Server.Id, new TransferServerOwnerDto { UserId = owner.Id }));
            await Assert.ThrowsAsync<BadRequestException>(() => servers.TransferOwnerAsync(owner.Id, created.Server.Id, new TransferServerOwnerDto { UserId = " " }));
            await Assert.ThrowsAsync<ForbiddenException>(() => servers.TransferOwnerAsync(stranger.Id, created.Server.Id, new TransferServerOwnerDto { UserId = stranger.Id }));
        }
    }
}
