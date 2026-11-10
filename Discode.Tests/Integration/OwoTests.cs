using Domain.Constants;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerMessages;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class OwoTests
    {
        private static async Task<(TestHost host, Domain.Entities.AppUser owner, Domain.Entities.AppUser friend, int serverId)> SetupAsync()
        {
            var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "friend@test.discode");
            var created = await host.CreateServerAsync(owner);
            await host.Get<IServerService>().JoinAsync(friend.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            return (host, owner, friend, created.Server.Id);
        }

        [Fact]
        public async Task Plain_messages_are_ignored_and_the_basic_commands_answer()
        {
            var (host, owner, _, serverId) = await SetupAsync();
            using var _ = host;
            var owo = host.Get<IOwoService>();

            Assert.Null(await owo.PlayAsync(serverId, owner.Id, "hello there"));
            Assert.Null(await owo.PlayAsync(serverId, owner.Id, "owoify this"));
            Assert.Contains("owo hunt", (await owo.PlayAsync(serverId, owner.Id, "owo help"))!);
            Assert.Contains("100 cowoncy", (await owo.PlayAsync(serverId, owner.Id, "OwO cash"))!);
            Assert.Contains("do not know", (await owo.PlayAsync(serverId, owner.Id, "owo dance"))!);

            var daily = await owo.PlayAsync(serverId, owner.Id, "owo daily");
            Assert.Contains("260", daily!);
            Assert.Contains("360 cowoncy", (await owo.PlayAsync(serverId, owner.Id, "owo cash"))!);
            Assert.Contains("wait", (await owo.PlayAsync(serverId, owner.Id, "owo daily"))!);
        }

        [Fact]
        public async Task Hunting_costs_cowoncy_fills_the_zoo_and_animals_can_be_sold()
        {
            var (host, owner, _, serverId) = await SetupAsync();
            using var _ = host;
            var owo = host.Get<IOwoService>();

            Assert.Contains("empty", (await owo.PlayAsync(serverId, owner.Id, "owo zoo"))!);
            Assert.Contains("found", (await owo.PlayAsync(serverId, owner.Id, "owo hunt"))!);
            Assert.Contains("slow down", (await owo.PlayAsync(serverId, owner.Id, "owo hunt"))!);
            Assert.Contains("Total:", (await owo.PlayAsync(serverId, owner.Id, "owo zoo"))!);
            Assert.Contains("95 cowoncy", (await owo.PlayAsync(serverId, owner.Id, "owo cash"))!);
            Assert.Contains("sold", (await owo.PlayAsync(serverId, owner.Id, "owo sell all"))!);
            Assert.Contains("nothing", (await owo.PlayAsync(serverId, owner.Id, "owo sell all"))!);
            Assert.Contains("tell me what to sell", (await owo.PlayAsync(serverId, owner.Id, "owo sell"))!);
        }

        [Fact]
        public async Task Giving_and_betting_move_cowoncy_within_the_limits()
        {
            var (host, owner, friend, serverId) = await SetupAsync();
            using var _ = host;
            var owo = host.Get<IOwoService>();

            Assert.Contains("sent", (await owo.PlayAsync(serverId, owner.Id, "owo give @" + friend.UserName + " 40"))!);
            Assert.Contains("60 cowoncy", (await owo.PlayAsync(serverId, owner.Id, "owo cash"))!);
            Assert.Contains("140 cowoncy", (await owo.PlayAsync(serverId, friend.Id, "owo cash"))!);
            Assert.Contains("only have", (await owo.PlayAsync(serverId, owner.Id, "owo give @" + friend.UserName + " 9999"))!);
            Assert.Contains("yourself", (await owo.PlayAsync(serverId, owner.Id, "owo give @" + owner.UserName + " 5"))!);
            Assert.Contains("could not find", (await owo.PlayAsync(serverId, owner.Id, "owo give @nobody 5"))!);
            Assert.Contains("only have", (await owo.PlayAsync(serverId, owner.Id, "owo cf 5000"))!);
            Assert.Contains("at least 1", (await owo.PlayAsync(serverId, owner.Id, "owo cf 0"))!);

            var before = 60;
            var flip = await owo.PlayAsync(serverId, owner.Id, "owo cf 10");
            Assert.Contains("coin spins", flip!);
            var cash = (await owo.PlayAsync(serverId, owner.Id, "owo cash"))!;
            Assert.True(cash.Contains((before + 10) + " cowoncy") || cash.Contains((before - 10) + " cowoncy"), cash);

            var top = await owo.PlayAsync(serverId, owner.Id, "owo top");
            Assert.Contains("Richest", top!);
            Assert.Contains(friend.DisplayName, top!);
        }

        [Fact]
        public async Task Only_the_owner_turns_the_game_off_and_then_it_stays_quiet()
        {
            var (host, owner, friend, serverId) = await SetupAsync();
            using var _ = host;
            var owo = host.Get<IOwoService>();

            await Assert.ThrowsAsync<ForbiddenException>(() => owo.SetEnabledAsync(friend.Id, serverId, false));
            await owo.SetEnabledAsync(owner.Id, serverId, false);
            Assert.Null(await owo.PlayAsync(serverId, owner.Id, "owo cash"));
            await owo.SetEnabledAsync(owner.Id, serverId, true);
            Assert.NotNull(await owo.PlayAsync(serverId, owner.Id, "owo cash"));
        }

        [Fact]
        public async Task A_channel_message_that_starts_with_owo_gets_an_answer_from_the_OwO_bot()
        {
            var (host, owner, _, serverId) = await SetupAsync();
            using var _ = host;
            var channels = await host.Get<IServerChannelService>().GetAsync(owner.Id, serverId);
            var textId = channels.Categories.SelectMany(c => c.Channels).Concat(channels.Loose).First(c => c.Kind == "text").Id;
            var change = await host.Get<IServerMessageService>().SendAsync(owner.Id, serverId, textId, new SendServerMessageDto { Text = "owo cash" });
            var reply = Assert.Single(change.Replies);
            Assert.Equal("owo", reply.SenderUserName);
            Assert.True(reply.SenderIsBot);
            Assert.Contains("100 cowoncy", reply.Text);

            var normal = await host.Get<IServerMessageService>().SendAsync(owner.Id, serverId, textId, new SendServerMessageDto { Text = "just chatting" });
            Assert.Empty(normal.Replies);
        }
    }
}
