namespace Service.Helpers.DTOs.DirectMessages
{
    public class EditedMessageDto
    {
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public MessageDto MessageForSender { get; set; } = null!;
        public MessageDto MessageForRecipient { get; set; } = null!;
    }
}
