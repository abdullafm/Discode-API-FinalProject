using Domain.Constants;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class GroupShareTests
    {
        private static async Task<(TestHost host, Domain.Entities.AppUser me, Domain.Entities.AppUser a, Domain.Entities.AppUser b, GroupDto crew, GroupDto club)> SetupAsync()
        {
            var host = new TestHost();
            var me = await host.AddUserAsync(Roles.Member, "me@test.discode");
            var a = await host.AddUserAsync(Roles.Member, "alpha@test.discode");
            var b = await host.AddUserAsync(Roles.Member, "beta@test.discode");
            await host.MakeFriendsAsync(me, a);
            await host.MakeFriendsAsync(me, b);
            var c = await host.AddUserAsync(Roles.Member, "gamma@test.discode");
            await host.MakeFriendsAsync(me, c);
            var groups = host.Get<IGroupService>();
            var crew = await groups.CreateAsync(me.Id, new CreateGroupDto { Name = "Crew", UserNames = { a.UserName!, b.UserName! } });
            var club = await groups.CreateAsync(me.Id, new CreateGroupDto { Name = "Club", UserNames = { b.UserName!, c.UserName! } });
            return (host, me, a, b, crew, club);
        }

        [Fact]
        public async Task A_post_can_be_shared_with_groups_and_people_in_one_go()
        {
            var (host, me, a, _, crew, club) = await SetupAsync();
            using var _host = host;
            var posts = host.Get<IPostService>();
            var post = await posts.CreateAsync(me.Id, new CreatePostDto { Text = "look" });
            var model = new SharePostDto { UserNames = { a.UserName! }, GroupIds = { crew.Id, club.Id }, Note = "enjoy" };

            var direct = (await posts.ShareAsync(me.Id, post.Id, model)).ToList();
            var groups = await posts.ShareToGroupsAsync(me.Id, post.Id, model);

            Assert.Equal(2, direct.Count);
            Assert.Equal(4, groups.Count);
            var chat = await host.Get<IGroupService>().GetMessagesAsync(me.Id, crew.Id);
            Assert.Contains(chat, m => m.Text.Contains("/Social/Index?post=" + post.Id));
            Assert.Contains(chat, m => m.Text == "enjoy");

            var onlyGroups = new SharePostDto { GroupIds = { crew.Id } };
            Assert.Empty(await posts.ShareAsync(me.Id, post.Id, onlyGroups));
            Assert.Single((await posts.ShareToGroupsAsync(me.Id, post.Id, onlyGroups)));
            await Assert.ThrowsAsync<BadRequestException>(() => posts.ShareAsync(me.Id, post.Id, new SharePostDto()));
        }

        [Fact]
        public async Task Sharing_into_a_group_needs_membership_and_at_most_ten_groups()
        {
            var (host, me, a, _, crew, _) = await SetupAsync();
            using var _host = host;
            var posts = host.Get<IPostService>();
            var post = await posts.CreateAsync(a.Id, new CreatePostDto { Text = "mine" });

            await Assert.ThrowsAnyAsync<Exception>(() => posts.ShareToGroupsAsync(me.Id, post.Id, new SharePostDto { GroupIds = { 99999 } }));
            var many = new SharePostDto();
            for (var i = 1; i <= 11; i++)
            {
                many.GroupIds.Add(crew.Id + i);
            }

            await Assert.ThrowsAsync<BadRequestException>(() => posts.ShareToGroupsAsync(me.Id, post.Id, many));
        }

        [Fact]
        public async Task A_message_can_be_forwarded_from_a_chat_or_a_group_into_groups()
        {
            var (host, me, a, _, crew, club) = await SetupAsync();
            using var _host = host;
            var dms = host.Get<IDirectMessageService>();
            var groups = host.Get<IGroupService>();
            var share = host.Get<IGroupShareService>();

            var sent = await dms.SendAsync(a.Id, me.UserName!, new SendMessageDto { Text = "secret plan" });
            var changes = await share.ForwardFromDmAsync(me.Id, sent.MessageForRecipient.Id, new[] { crew.Id }, "fyi");
            Assert.Equal(2, changes.Count);
            var crewChat = await groups.GetMessagesAsync(me.Id, crew.Id);
            Assert.Contains(crewChat, m => m.Text.Contains("Forwarded from") && m.Text.Contains("secret plan"));
            Assert.Contains(crewChat, m => m.Text == "fyi");

            var original = crewChat.First(m => m.Text == "fyi");
            var again = await share.ForwardFromGroupAsync(me.Id, crew.Id, original.Id, new[] { club.Id }, string.Empty);
            Assert.Single(again);
            Assert.Contains(await groups.GetMessagesAsync(me.Id, club.Id), m => m.Text.Contains("Forwarded from") && m.Text.Contains("fyi"));
            Assert.Empty(await share.ForwardFromDmAsync(me.Id, sent.MessageForRecipient.Id, new List<int>(), string.Empty));
        }
    }
}
