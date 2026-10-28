using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.DTOs.ServerMessages
{
    public class ServerMessageDto
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public int ServerId { get; set; }
        public int ChannelId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderUserName { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public ReplyDto? ReplyTo { get; set; }
        public bool IsPoll { get; set; }
        public PollDto? Poll { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
        public List<ReactionDto> Reactions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
