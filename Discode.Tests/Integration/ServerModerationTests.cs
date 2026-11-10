using Domain.Constants;
using Service.Helpers.DTOs.ServerAudit;
using Service.Helpers.DTOs.ServerBans;
using Service.Helpers.DTOs.ServerPrune;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerModerationTests
    {
        [Fact]
        public async Task Ban_lists_start_empty_for_the_owner()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            Assert.Empty(await host.Get<IServerBanService>().GetBansAsync(owner.Id, created.Server.Id, new ServerBanFilterDto()));
            Assert.Contains(owner.Id, await host.Get<IServerBanService>().GetMemberIdsAsync(created.Server.Id));
            Assert.Contains(owner.Id, await host.Get<IServerTimeoutService>().GetMemberIdsAsync(created.Server.Id));
        }

        [Fact]
        public async Task You_cannot_ban_yourself()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IServerBanService>().BanAsync(owner.Id, created.Server.Id, new BanServerMemberDto { UserId = owner.Id }));

            Assert.Contains("yourself", error.Message);
        }

        [Fact]
        public async Task Audit_log_and_prune_preview_are_available_to_the_owner()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            var logs = (await host.Get<IServerAuditService>().GetAsync(owner.Id, created.Server.Id, new ServerAuditFilterDto())).ToList();
            Assert.NotEmpty(logs);

            var preview = await host.Get<IServerPruneService>().PreviewAsync(owner.Id, created.Server.Id, new ServerPruneDto { Days = 7 });
            Assert.Equal(0, preview);

            var pruned = await host.Get<IServerPruneService>().PruneAsync(owner.Id, created.Server.Id, new ServerPruneDto { Days = 7 });
            Assert.Equal(0, pruned.Count);
            Assert.Contains(owner.Id, pruned.RemainingUserIds);
        }

        [Fact]
        public async Task Everyone_role_exists_on_a_new_server()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            var roles = (await host.Get<IServerRoleService>().GetRolesAsync(owner.Id, created.Server.Id)).ToList();
            Assert.Contains(roles, row => row.IsEveryone);
        }
    }
}
