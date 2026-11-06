namespace Service.Helpers.DTOs.Groups
{
    public class GroupMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
    }
}
