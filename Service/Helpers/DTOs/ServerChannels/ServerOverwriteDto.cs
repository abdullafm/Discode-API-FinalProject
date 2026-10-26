namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerOverwriteDto
    {
        public string TargetType { get; set; } = "role";
        public string TargetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "gray";
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsEveryone { get; set; }
        public long Allow { get; set; }
        public long Deny { get; set; }
    }
}
