using Domain.Constants;
using Domain.Entities;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class BlockTests
    {
        private static SendFriendRequestDto FriendTo(AppUser user) => new() { UserName = user.UserName };
        private static BlockUserDto BlockOf(AppUser user) => new() { UserName = user.UserName };

        [Fact]
        public async Task Blocking_a_friend_removes_the_friendship()
        {
            using var host = new TestHost();
            var left = await host.AddUserAsync(Roles.Member, "left@test.discode");
            var right = await host.AddUserAsync(Roles.Member, "right@test.discode");

            var friends = host.Get<IFriendRequestService>();
            var request = await friends.SendAsync(left.Id, FriendTo(right));
            await friends.AcceptAsync(right.Id, request.Id);

            await host.Get<IUserBlockService>().BlockAsync(left.Id, BlockOf(right));

            Assert.Empty(await friends.GetAllAsync(left.Id));
            Assert.Empty(await friends.GetAllAsync(right.Id));
            Assert.Single(await host.Get<IUserBlockService>().GetAllAsync(left.Id));
        }

        [Fact]
        public async Task A_block_stops_a_new_friend_request()
        {
            using var host = new TestHost();
            var left = await host.AddUserAsync(Roles.Member, "left@test.discode");
            var right = await host.AddUserAsync(Roles.Member, "right@test.discode");

            await host.Get<IUserBlockService>().BlockAsync(left.Id, BlockOf(right));

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IFriendRequestService>().SendAsync(right.Id, FriendTo(left)));

            Assert.Contains("cannot send a friend request", error.Message);
        }

        [Fact]
        public async Task You_cannot_block_yourself()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IUserBlockService>().BlockAsync(user.Id, BlockOf(user)));

            Assert.Contains("yourself", error.Message);
        }

        [Fact]
        public async Task A_block_can_be_lifted()
        {
            using var host = new TestHost();
            var left = await host.AddUserAsync(Roles.Member, "left@test.discode");
            var right = await host.AddUserAsync(Roles.Member, "right@test.discode");
            var blocks = host.Get<IUserBlockService>();

            var blocked = await blocks.BlockAsync(left.Id, BlockOf(right));
            await blocks.UnblockAsync(left.Id, blocked.Id);

            Assert.Empty(await blocks.GetAllAsync(left.Id));
        }
    }
}
