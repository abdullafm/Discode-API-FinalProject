namespace Service.Helpers.DTOs.DirectMessages
{
    public class ReactionUpdatedDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public MessageReactionsDto ForUser { get; set; } = null!;
        public MessageReactionsDto ForRecipient { get; set; } = null!;
    }
}
