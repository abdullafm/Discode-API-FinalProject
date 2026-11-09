using Domain.Constants;
using Domain.Entities;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class FriendRequestTests
    {
        private static SendFriendRequestDto To(AppUser user) => new() { UserName = user.UserName };

        [Fact]
        public async Task A_new_friend_request_notifies_the_receiver_but_nobody_else()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");

            await host.Get<IFriendRequestService>().SendAsync(sender.Id, To(receiver));

            var notifications = host.Get<INotificationService>();
            var forReceiver = Assert.Single(await notifications.GetAsync(receiver.Id));
            Assert.Equal("FriendRequest", forReceiver.Type);
            Assert.Equal(sender.UserName, forReceiver.ActorUserName);

            Assert.Empty(await notifications.GetAsync(sender.Id));
            Assert.Empty(await notifications.GetAsync(stranger.Id));
        }

        [Fact]
        public async Task A_refused_request_notifies_nobody()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            var friends = host.Get<IFriendRequestService>();
            var request = await friends.SendAsync(sender.Id, To(receiver));
            await friends.DeclineAsync(receiver.Id, request.Id);

            Assert.Empty(await host.Get<INotificationService>().GetAsync(receiver.Id));
            Assert.Empty(await host.Get<INotificationService>().GetAsync(sender.Id));
            Assert.Empty((await friends.GetPendingAsync(receiver.Id)).Received);
        }

        [Fact]
        public async Task Accepting_a_request_notifies_the_sender_and_makes_them_friends()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            var friends = host.Get<IFriendRequestService>();
            var request = await friends.SendAsync(sender.Id, To(receiver));
            await friends.AcceptAsync(receiver.Id, request.Id);

            Assert.Single(await friends.GetAllAsync(sender.Id));
            Assert.Single(await friends.GetAllAsync(receiver.Id));

            var forSender = Assert.Single(await host.Get<INotificationService>().GetAsync(sender.Id));
            Assert.Equal("FriendAccepted", forSender.Type);
            Assert.Empty(await host.Get<INotificationService>().GetAsync(receiver.Id));
        }

        [Fact]
        public async Task You_cannot_add_yourself()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IFriendRequestService>().SendAsync(user.Id, To(user)));

            Assert.Contains("yourself", error.Message);
        }

        [Fact]
        public async Task A_second_request_is_rejected()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var friends = host.Get<IFriendRequestService>();

            await friends.SendAsync(sender.Id, To(receiver));

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                friends.SendAsync(sender.Id, To(receiver)));

            Assert.Contains("already sent", error.Message);
        }

        [Fact]
        public async Task A_pending_request_can_be_cancelled()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var friends = host.Get<IFriendRequestService>();

            var request = await friends.SendAsync(sender.Id, To(receiver));
            await friends.CancelAsync(sender.Id, request.Id);

            Assert.Empty((await friends.GetPendingAsync(receiver.Id)).Received);
            Assert.Empty(await host.Get<INotificationService>().GetAsync(receiver.Id));
        }

        [Fact]
        public async Task An_accepted_friend_can_be_removed()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var friends = host.Get<IFriendRequestService>();

            var request = await friends.SendAsync(sender.Id, To(receiver));
            await friends.AcceptAsync(receiver.Id, request.Id);
            await friends.RemoveAsync(sender.Id, request.Id);

            Assert.Empty(await friends.GetAllAsync(sender.Id));
            Assert.Empty(await friends.GetUserFriendsAsync(receiver.Id, sender.UserName!));
        }

        [Fact]
        public async Task A_pending_request_shows_up_on_both_sides()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            await host.Get<IFriendRequestService>().SendAsync(sender.Id, To(receiver));

            var pendingForSender = await host.Get<IFriendRequestService>().GetPendingAsync(sender.Id);
            var pendingForReceiver = await host.Get<IFriendRequestService>().GetPendingAsync(receiver.Id);
            Assert.Equal(receiver.UserName, Assert.Single(pendingForSender.Sent).UserName);
            Assert.Equal(sender.UserName, Assert.Single(pendingForReceiver.Received).UserName);
        }
    }
}
