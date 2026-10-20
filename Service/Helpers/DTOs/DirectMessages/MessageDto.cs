namespace Service.Helpers.DTOs.DirectMessages
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderUserName { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Mine { get; set; }
        public List<ReactionDto> Reactions { get; set; } = new();
        public ReplyDto? ReplyTo { get; set; }
    }
}
