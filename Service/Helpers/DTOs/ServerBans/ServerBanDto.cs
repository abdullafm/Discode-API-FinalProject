namespace Service.Helpers.DTOs.ServerBans
{
    public class ServerBanDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string BannedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
