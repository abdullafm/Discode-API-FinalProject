namespace Service.Helpers.DTOs.DirectMessages
{
    public class SendMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public int? ReplyToMessageId { get; set; }
    }
}
