using Domain.Configurations;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<UserIgnore> UserIgnores { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<ServerMember> ServerMembers { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostRepost> PostReposts { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryLike> StoryLikes { get; set; }
        public DbSet<StoryView> StoryViews { get; set; }
        public DbSet<LiveStream> LiveStreams { get; set; }
        public DbSet<EmojiCategory> EmojiCategories { get; set; }
        public DbSet<Emoji> Emojis { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<MessageForward> MessageForwards { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<Call> Calls { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<GroupMessageAttachment> GroupMessageAttachments { get; set; }
        public DbSet<GroupMessageReaction> GroupMessageReactions { get; set; }
        public DbSet<GroupCall> GroupCalls { get; set; }
        public DbSet<GroupCallParticipant> GroupCallParticipants { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }
    }
}
