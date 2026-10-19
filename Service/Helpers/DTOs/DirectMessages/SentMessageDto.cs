namespace Service.Helpers.DTOs.DirectMessages
{
    public class SentMessageDto
    {
        public string RecipientId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public MessageDto MessageForSender { get; set; } = null!;
        public MessageDto MessageForRecipient { get; set; } = null!;
        public ConversationDto ConversationForSender { get; set; } = null!;
        public ConversationDto ConversationForRecipient { get; set; } = null!;
    }
}
