using Domain.Constants;
using Domain.Enums;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class AdminModerationTests
    {
        [Fact]
        public async Task Staff_can_warn_timeout_kick_and_ban_a_member()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var moderation = host.Get<IUserModerationService>();
            var actor = TestHost.Staff(admin);

            var warned = await moderation.WarnAsync(actor, member.Id, new WarnUserDto { Reason = "spam" });
            Assert.Equal(1, warned.WarningCount);
            Assert.Equal(member.Id, warned.UserId);
            Assert.Single(await moderation.GetUnseenWarningsAsync(member.Id));

            var timed = await moderation.TimeoutAsync(actor, member.Id, new TimeoutUserDto { Reason = "cooldown", Minutes = 10 });
            Assert.NotNull(timed.TimeoutUntil);
            await moderation.ClearTimeoutAsync(actor, member.Id);

            var kicked = await moderation.KickAsync(actor, member.Id, new KickUserDto { Reason = "signed out" });
            Assert.True(kicked.ForceLogout);

            var banned = await moderation.BanAsync(actor, member.Id, new BanUserDto { Reason = "repeat spam", Days = 1 });
            Assert.True(banned.ForceLogout);
            await moderation.UnbanAsync(actor, member.Id);

            var detail = await moderation.GetAsync(actor, member.Id);
            Assert.Equal(member.UserName, detail.UserName);
            Assert.False(detail.IsBanned);
        }

        [Fact]
        public async Task Staff_cannot_moderate_themselves()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");

            var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
                host.Get<IUserModerationService>().WarnAsync(TestHost.Staff(admin), admin.Id, new WarnUserDto { Reason = "no" }));

            Assert.Contains("own account", error.Message);
        }

        [Fact]
        public async Task SuperAdmin_can_live_ban_and_log_content()
        {
            using var host = new TestHost();
            var super = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var moderation = host.Get<IUserModerationService>();
            var actor = TestHost.Super(super);

            var liveBan = await moderation.LiveBanAsync(actor, member.Id, new LiveBanDto { Reason = "tos", Minutes = 60 });
            Assert.Equal("tos", liveBan.Reason);
            await moderation.LiveUnbanAsync(actor, member.Id);

            var logged = await moderation.LogContentActionAsync(actor, member.Id, AdminActionKind.ContentDelete, "removed a post", "post #1");
            Assert.Equal("removed a post", logged.Reason);

            var activity = await moderation.GetActivityAsync(new AdminListFilterDto());
            Assert.True(activity.Total >= 1);
        }

        [Fact]
        public async Task Admin_overview_lists_users_and_servers()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(member);
            var adminService = host.Get<IAdminService>();

            var overview = await adminService.GetOverviewAsync();
            Assert.True(overview.Users.Total >= 2);
            Assert.True(overview.Servers.Total >= 1);

            var users = await adminService.GetUsersAsync(new AdminListFilterDto { Search = "member" });
            Assert.Contains(users.Items, row => row.Id == member.Id);

            var servers = await adminService.GetAllServersAsync(new AdminListFilterDto());
            Assert.Contains(servers.Items, row => row.Id == created.Server.Id);

            var detail = await adminService.GetServerAsync(created.Server.Id);
            Assert.Equal("Nexus", detail.Name);

            var members = await adminService.GetServerMembersAsync(created.Server.Id, new AdminListFilterDto());
            Assert.Contains(members.Items, row => row.UserName == member.UserName);

            var warned = await adminService.WarnServerAsync(admin.Id, created.Server.Id, new BanServerDto { Reason = "watch this" });
            Assert.Equal(member.Id, warned.OwnerId);
        }

        [Fact]
        public async Task Admin_content_lists_a_created_post()
        {
            using var host = new TestHost();
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var post = await host.Get<IPostService>().CreateAsync(member.Id, new CreatePostDto { Text = "report me" });

            var list = await host.Get<IAdminContentService>().GetAsync("posts", null, 1, 20);
            Assert.Contains(list.Items, row => row.Id == post.Id);

            var found = await host.Get<IAdminContentService>().FindAsync("post", post.Id);
            Assert.Equal(member.Id, found.AuthorId);
            Assert.Equal("post", found.Kind);
        }

        [Fact]
        public async Task Unban_fails_when_the_member_is_not_banned()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IUserModerationService>().UnbanAsync(TestHost.Staff(admin), member.Id));

            Assert.Contains("not banned", error.Message);
        }

        [Fact]
        public async Task Admin_can_search_servers_and_log_a_live_end()
        {
            using var host = new TestHost();
            var super = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(member, "Galaxy");

            var found = await host.Get<IAdminService>().SearchServersAsync(new AdminListFilterDto { Search = "Galaxy" });
            Assert.Contains(found.Items, row => row.Id == created.Server.Id);

            var logged = await host.Get<IUserModerationService>().LogLiveEndAsync(TestHost.Super(super), member.Id, "ended by staff", "Galaxy Live");
            Assert.Equal("ended by staff", logged.Reason);
        }
    }
}
