namespace Service.Helpers.DTOs.Groups
{
    public class GroupCallDto
    {
        public int CallId { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupIconUrl { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public bool Video { get; set; }
        public bool Active { get; set; }
        public DateTime StartedAt { get; set; }
        public string StarterUserName { get; set; } = string.Empty;
        public string StarterDisplayName { get; set; } = string.Empty;
        public string StarterAvatarUrl { get; set; } = string.Empty;
        public List<GroupCallParticipantDto> Participants { get; set; } = new();
    }
}
