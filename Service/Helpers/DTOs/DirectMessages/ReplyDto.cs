namespace Service.Helpers.DTOs.DirectMessages
{
    public class ReplyDto
    {
        public int MessageId { get; set; }
        public string SenderUserName { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
