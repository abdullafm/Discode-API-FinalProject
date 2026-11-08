namespace Service.Helpers.DTOs.Presence
{
    // Who has to be told that somebody is typing, and what to call the chat for them
    public class TypingDto
    {
        public List<string> AudienceIds { get; set; } = new();
        public string Kind { get; set; } = string.Empty;
        // the key the receivers use for this chat (a direct chat is named after the sender for the other person)
        public string Key { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
