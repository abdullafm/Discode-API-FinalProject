using System.Text.Json.Serialization;

namespace Service.Helpers.DTOs.DirectMessages
{
    public class ForwardMessageDto
    {
        public List<string> UserNames { get; set; } = new();
        public string Note { get; set; } = string.Empty;

        [JsonIgnore]
        public string SenderId { get; set; } = string.Empty;
        [JsonIgnore]
        public string RecipientId { get; set; } = string.Empty;
        [JsonIgnore]
        public MessageDto? MessageForSender { get; set; }
        [JsonIgnore]
        public MessageDto? MessageForRecipient { get; set; }
        [JsonIgnore]
        public ConversationDto? ConversationForSender { get; set; }
        [JsonIgnore]
        public ConversationDto? ConversationForRecipient { get; set; }
    }
}
