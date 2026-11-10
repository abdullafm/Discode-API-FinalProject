using Domain.Constants;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.DTOs.Stories;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class DirectMessageTests
    {
        [Fact]
        public async Task Friends_can_send_a_text_message()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            var friends = host.Get<IFriendRequestService>();
            var request = await friends.SendAsync(sender.Id, new SendFriendRequestDto { UserName = receiver.UserName });
            await friends.AcceptAsync(receiver.Id, request.Id);

            var sent = await host.Get<IDirectMessageService>().SendAsync(sender.Id, receiver.UserName!, new SendMessageDto
            {
                Text = "hello there"
            });

            Assert.Equal("hello there", sent.MessageForSender.Text);
            Assert.False(sent.MessageForSender.Undelivered);
            Assert.Equal(receiver.Id, sent.RecipientId);

            var conversations = (await host.Get<IDirectMessageService>().GetConversationsAsync(receiver.Id)).ToList();
            Assert.Contains(conversations, row => row.UserName == sender.UserName && row.LastMessage == "hello there");
        }

        [Fact]
        public async Task A_blocked_user_does_not_receive_the_message()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");

            await host.Get<IUserBlockService>().BlockAsync(receiver.Id, new BlockUserDto { UserName = sender.UserName });

            var sent = await host.Get<IDirectMessageService>().SendAsync(sender.Id, receiver.UserName!, new SendMessageDto
            {
                Text = "are you there"
            });

            Assert.True(sent.MessageForSender.Undelivered);
            Assert.Empty(await host.Get<IDirectMessageService>().GetConversationsAsync(receiver.Id));
        }

        [Fact]
        public async Task A_message_can_be_edited_reacted_to_and_pinned()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            await host.MakeFriendsAsync(sender, receiver);
            var dms = host.Get<IDirectMessageService>();

            var sent = await dms.SendAsync(sender.Id, receiver.UserName!, new SendMessageDto { Text = "draft" });
            var id = sent.MessageForSender.Id;

            var edited = await dms.EditAsync(sender.Id, id, new SendMessageDto { Text = "final" });
            Assert.Equal("final", edited.MessageForSender.Text);
            Assert.NotNull(edited.MessageForSender.EditedAt);

            var reacted = await dms.ReactAsync(receiver.Id, id, new ToggleReactionDto { Emoji = "👍" });
            Assert.Contains(reacted.ForUser.Reactions, row => row.Emoji == "👍" && row.Reacted);

            var pinned = await dms.PinAsync(sender.Id, id, new PinMessageDto { Pinned = true });
            Assert.NotNull(pinned.ForUser.PinnedAt);
            Assert.Single(await dms.GetPinsAsync(sender.Id, receiver.UserName!));
        }

        [Fact]
        public async Task A_poll_can_be_sent_and_voted_on()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            await host.MakeFriendsAsync(sender, receiver);
            var dms = host.Get<IDirectMessageService>();

            var poll = await dms.SendPollAsync(sender.Id, receiver.UserName!, TestHost.Poll());
            Assert.True(poll.MessageForSender.IsPoll);
            var optionId = poll.MessageForRecipient.Poll!.Options[0].Id;

            var voted = await dms.VotePollAsync(receiver.Id, poll.MessageForSender.Id, new VotePollDto { OptionIds = { optionId } });
            Assert.True(voted.ForUser.Voted);
            Assert.Equal(1, voted.ForUser.TotalVoters);
        }

        [Fact]
        public async Task Chat_relation_and_system_messages_are_available()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            await host.MakeFriendsAsync(sender, receiver);
            var dms = host.Get<IDirectMessageService>();

            await dms.SendAsync(sender.Id, receiver.UserName!, new SendMessageDto { Text = "hey" });
            await dms.MarkReadAsync(receiver.Id, sender.UserName!);

            var chat = await dms.GetChatAsync(receiver.Id, sender.UserName!);
            Assert.True(chat.IsFriend);
            Assert.Contains(chat.Messages, row => row.Text == "hey");

            var relation = await dms.GetRelationAsync(sender.Id, receiver.UserName!);
            Assert.Equal("friend", relation.Relation);

            var system = await dms.SendSystemAsync(receiver.Id, "Welcome to Discode.");
            Assert.Contains("Welcome", system.MessageForRecipient.Text);

            await dms.CloseAsync(receiver.Id, sender.UserName!);
            Assert.DoesNotContain(await dms.GetConversationsAsync(receiver.Id), row => row.UserName == sender.UserName);
        }

        [Fact]
        public async Task A_message_can_be_forwarded_to_another_friend()
        {
            using var host = new TestHost();
            var sender = await host.AddUserAsync(Roles.Member, "sender@test.discode");
            var receiver = await host.AddUserAsync(Roles.Member, "receiver@test.discode");
            var third = await host.AddUserAsync(Roles.Member, "third@test.discode");
            await host.MakeFriendsAsync(sender, receiver);
            await host.MakeFriendsAsync(sender, third);
            var dms = host.Get<IDirectMessageService>();

            var sent = await dms.SendAsync(sender.Id, receiver.UserName!, new SendMessageDto { Text = "pass this on" });
            var forwarded = (await dms.ForwardAsync(sender.Id, sent.MessageForSender.Id, new ForwardMessageDto
            {
                UserNames = { third.UserName! }
            })).ToList();

            Assert.Contains(forwarded, row => row.RecipientId == third.Id && row.MessageForRecipient.Text == "pass this on");
        }

        [Fact]
        public async Task A_story_reply_and_a_live_share_open_a_chat()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var viewer = await host.AddUserAsync(Roles.Member, "viewer@test.discode");
            await host.MakeFriendsAsync(author, viewer);
            var dms = host.Get<IDirectMessageService>();

            var story = await host.Get<IStoryService>().CreateAsync(author.Id, new CreateStoryDto { ImageUrl = "/uploads/stories/demo.jpg" });
            var reply = await dms.SendStoryReplyAsync(viewer.Id, story.Id, "nice shot");
            Assert.Equal(story.Id, reply.MessageForSender.StoryId);
            Assert.Equal("nice shot", reply.MessageForRecipient.Text);

            var share = await dms.SendLiveShareAsync(author.Id, viewer.UserName!, new LiveShareDto
            {
                StreamId = 9,
                Title = "Friday raid",
                HostName = author.DisplayName
            });
            Assert.Equal(9, share.MessageForRecipient.LiveStreamId);
            Assert.Equal("Friday raid", share.MessageForRecipient.LiveTitle);
        }
    }
}
