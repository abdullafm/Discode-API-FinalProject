namespace Service.Helpers.DTOs.Groups
{
    public class GroupCallParticipantDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool Screen { get; set; }
    }

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

    public class GroupCallBriefDto
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public bool Video { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class GroupCallChangeDto
    {
        public GroupCallDto Call { get; set; } = new();
        public List<string> MemberIds { get; set; } = new();
        public bool Started { get; set; }
        public bool Ended { get; set; }
        public GroupMessageDto? Message { get; set; }
        public List<GroupCallChangeDto> Left { get; set; } = new();
    }
}
