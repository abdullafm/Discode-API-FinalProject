namespace Service.Helpers.DTOs.DirectMessages
{
    public class PinUpdatedDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public MessageDto ForUser { get; set; } = null!;
        public MessageDto ForRecipient { get; set; } = null!;
    }
}
