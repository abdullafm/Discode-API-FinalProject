namespace Service.Helpers.DTOs.Accounts
{
    public class UserProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public DateTime MemberSince { get; set; }
        public bool IsPrivate { get; set; }
        public string Relation { get; set; } = "none";
        public int? RequestId { get; set; }
    }
}
