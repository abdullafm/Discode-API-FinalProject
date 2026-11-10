using Domain.Constants;
using Service.Helpers.DTOs.ServerChannels;
using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.DTOs.ServerTemplates;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerTemplateTests
    {
        [Fact]
        public async Task A_template_builds_a_new_server_with_the_same_roles_categories_and_channels()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var other = await host.AddUserAsync(Roles.Member, "other@test.discode");
            var created = await host.CreateServerAsync(owner, "Study Group");
            var serverId = created.Server.Id;
            var channels = host.Get<IServerChannelService>();

            var category = await channels.CreateCategoryAsync(owner.Id, serverId, new CreateServerCategoryDto { Name = "Lessons", IsPrivate = false });
            await channels.CreateChannelAsync(owner.Id, serverId, new CreateServerChannelDto { Name = "math", Kind = "text", CategoryId = category.Id, SlowModeSeconds = 30 });
            await channels.CreateChannelAsync(owner.Id, serverId, new CreateServerChannelDto { Name = "study-room", Kind = "voice", CategoryId = category.Id, UserLimit = 5 });

            var templates = host.Get<IServerTemplateService>();
            Assert.Null(await templates.GetAsync(owner.Id, serverId));
            var saved = await templates.SaveAsync(owner.Id, serverId, new SaveServerTemplateDto { Title = "Class", Description = "A class server" });
            Assert.Equal(12, saved.Code.Length);
            Assert.Equal(0, saved.RoleCount);
            Assert.False(saved.OutOfDate);

            var info = await templates.GetInfoAsync(saved.Code);
            Assert.Equal("Study Group", info.ServerName);
            Assert.Contains(info.Categories, c => c.Name == "Lessons" && c.Channels.Count == 2);

            var made = await templates.UseAsync(other.Id, saved.Code, new UseServerTemplateDto { Name = "My Class" });
            Assert.Equal("My Class", made.Server.Name);
            Assert.True(made.Server.IsOwner);
            var copy = await channels.GetAsync(other.Id, made.Server.Id);
            var lessons = Assert.Single(copy.Categories, c => c.Name == "Lessons");
            Assert.Contains(lessons.Channels, c => c.Name == "math" && c.Kind == "text" && c.SlowModeSeconds == 30);
            Assert.Contains(lessons.Channels, c => c.Kind == "voice" && c.UserLimit == 5);
            Assert.DoesNotContain(copy.Loose, c => c.Name == "general");
            Assert.Equal(1, (await templates.GetInfoAsync(saved.Code)).UsageCount);
        }

        [Fact]
        public async Task Only_people_who_manage_the_server_edit_the_template_and_a_change_marks_it_out_of_date()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var created = await host.CreateServerAsync(owner);
            var serverId = created.Server.Id;
            await host.Get<IServerService>().JoinAsync(member.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var templates = host.Get<IServerTemplateService>();

            await Assert.ThrowsAsync<ForbiddenException>(() => templates.SaveAsync(member.Id, serverId, new SaveServerTemplateDto { Title = "x" }));
            await Assert.ThrowsAsync<BadRequestException>(() => templates.SaveAsync(owner.Id, serverId, new SaveServerTemplateDto { Title = " " }));
            var saved = await templates.SaveAsync(owner.Id, serverId, new SaveServerTemplateDto { Title = "Mine" });

            await host.Get<IServerChannelService>().CreateChannelAsync(owner.Id, serverId, new CreateServerChannelDto { Name = "extra", Kind = "text" });
            Assert.True((await templates.GetAsync(owner.Id, serverId))!.OutOfDate);
            var synced = await templates.SyncAsync(owner.Id, serverId);
            Assert.False(synced.OutOfDate);
            Assert.Equal(saved.Code, synced.Code);

            await Assert.ThrowsAsync<NotFoundException>(() => templates.GetInfoAsync("nope"));
            await Assert.ThrowsAsync<NotFoundException>(() => templates.UseAsync(member.Id, "nope", new UseServerTemplateDto { Name = "x" }));
            await templates.DeleteAsync(owner.Id, serverId);
            Assert.Null(await templates.GetAsync(owner.Id, serverId));
        }
    }
}
