namespace Service.Helpers.DTOs.Live
{
    // The host asks a viewer to come on stream as a guest
    public class LiveGuestInviteDto
    {
        public int StreamId { get; set; }
        public string TargetConnectionId { get; set; } = string.Empty;
        public string HostUserName { get; set; } = string.Empty;
        public string HostDisplayName { get; set; } = string.Empty;
        public string HostAvatarUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    // What happened to an invitation: the host hears about it, and the guest (if there is one) starts
    public class LiveGuestInviteAnswerDto
    {
        public string HostConnectionId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public LiveGuestDto? Guest { get; set; }
    }
}
