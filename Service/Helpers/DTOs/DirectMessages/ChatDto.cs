namespace Service.Helpers.DTOs.DirectMessages
{
    public class ChatDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public string Bio { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; }
        public bool IsFriend { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
    }
}
