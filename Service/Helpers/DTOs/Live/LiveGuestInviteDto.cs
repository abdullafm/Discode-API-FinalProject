namespace Service.Helpers.DTOs.Live
{
    public class LiveGuestInviteDto
    {
        public int StreamId { get; set; }
        public string TargetConnectionId { get; set; } = string.Empty;
        public string HostUserName { get; set; } = string.Empty;
        public string HostDisplayName { get; set; } = string.Empty;
        public string HostAvatarUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class LiveGuestInviteAnswerDto
    {
        public string HostConnectionId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public LiveGuestDto? Guest { get; set; }
    }
}
