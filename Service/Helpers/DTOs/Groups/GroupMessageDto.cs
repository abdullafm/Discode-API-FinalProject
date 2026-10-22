namespace Service.Helpers.DTOs.Groups
{
    public class GroupMessageDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string SenderUserName { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
