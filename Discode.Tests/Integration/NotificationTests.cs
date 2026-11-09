using Domain.Constants;
using Service.Helpers.DTOs.Friends;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class NotificationTests
    {
        [Fact]
        public async Task Marking_a_notification_read_leaves_the_others()
        {
            using var host = new TestHost();
            var first = await host.AddUserAsync(Roles.Member, "first@test.discode");
            var second = await host.AddUserAsync(Roles.Member, "second@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var friends = host.Get<IFriendRequestService>();

            await friends.SendAsync(first.Id, new SendFriendRequestDto { UserName = receiver.UserName });
            await friends.SendAsync(second.Id, new SendFriendRequestDto { UserName = receiver.UserName });

            var notifications = host.Get<INotificationService>();
            var mine = (await notifications.GetAsync(receiver.Id)).ToList();
            Assert.Equal(2, mine.Count);

            await notifications.MarkReadAsync(receiver.Id, mine[0].Id);

            var after = (await notifications.GetAsync(receiver.Id)).ToList();
            Assert.Contains(after, item => item.Id == mine[0].Id && item.IsRead);
            Assert.Contains(after, item => item.Id == mine[1].Id && !item.IsRead);
        }

        [Fact]
        public async Task A_notification_can_be_deleted()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            await host.Get<IFriendRequestService>().SendAsync(sender.Id, new SendFriendRequestDto { UserName = receiver.UserName });
            var notifications = host.Get<INotificationService>();
            var item = Assert.Single(await notifications.GetAsync(receiver.Id));

            await notifications.DeleteAsync(receiver.Id, item.Id);

            Assert.Empty(await notifications.GetAsync(receiver.Id));
        }
    }
}
