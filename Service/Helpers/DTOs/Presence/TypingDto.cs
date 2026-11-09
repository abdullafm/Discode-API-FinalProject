namespace Service.Helpers.DTOs.Presence
{
    public class TypingDto
    {
        public List<string> AudienceIds { get; set; } = new();
        public string Kind { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
