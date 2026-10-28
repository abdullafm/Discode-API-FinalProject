using Domain.Common;

namespace Domain.Entities
{
    public class ServerMessage : BaseEntity
    {
        public string PublicId { get; set; } = Domain.Common.PublicIdGenerator.New();
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public int ChannelId { get; set; }
        public ServerChannel Channel { get; set; } = null!;
        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public int? ReplyToMessageId { get; set; }
        public ServerMessage? ReplyTo { get; set; }
        public Poll? Poll { get; set; }
        public ICollection<ServerMessageAttachment> Attachments { get; set; } = new List<ServerMessageAttachment>();
        public ICollection<ServerMessageReaction> Reactions { get; set; } = new List<ServerMessageReaction>();
    }
}
