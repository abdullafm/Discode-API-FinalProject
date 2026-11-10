using Domain.Constants;
using Service.Helpers.DTOs.ServerChannels;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ServerChannelTests
    {
        [Fact]
        public async Task Creating_a_server_seeds_default_channels()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);
            var channels = await host.Get<IServerChannelService>().GetAsync(owner.Id, created.Server.Id);

            Assert.Contains(channels.Categories, category => category.Channels.Any(row => row.Kind == "text" && row.Name == "general"));
            Assert.Contains(channels.Categories, category => category.Channels.Any(row => row.Kind == "voice"));
        }

        [Fact]
        public async Task The_owner_can_add_and_rename_channels()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);
            var channels = host.Get<IServerChannelService>();

            var category = await channels.CreateCategoryAsync(owner.Id, created.Server.Id, new CreateServerCategoryDto { Name = "Projects" });
            Assert.Equal("Projects", category.Name);

            var extra = await channels.CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto
            {
                Name = "dev",
                Kind = "text",
                CategoryId = category.Id
            });
            Assert.Equal("dev", extra.Name);
            Assert.Equal("text", extra.Kind);

            var viewers = await channels.GetViewerIdsAsync(owner.Id, created.Server.Id, extra.Id);
            Assert.Contains(owner.Id, viewers);

            var targets = await channels.SearchTargetsAsync(owner.Id, created.Server.Id, owner.UserName);
            Assert.Contains(targets.Members, row => row.UserName == owner.UserName);
        }

        [Fact]
        public async Task An_invalid_channel_kind_is_rejected()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IServerChannelService>().CreateChannelAsync(owner.Id, created.Server.Id, new CreateServerChannelDto
                {
                    Name = "weird",
                    Kind = "camera"
                }));

            Assert.Contains("valid channel type", error.Message);
        }
    }
}
