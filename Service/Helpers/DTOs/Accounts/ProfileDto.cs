namespace Service.Helpers.DTOs.Accounts
{
    public class ProfileDto
    {
        public string Email { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public bool HasNitro { get; set; }
        public bool IsBooster { get; set; }
        public string Pronouns { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public DateTime? CustomStatusClearAt { get; set; }
        public bool IsPublic { get; set; } = true;
        public string Theme { get; set; } = "light";
        public bool NotifyAll { get; set; } = true;
        public bool NotifyDm { get; set; } = true;
        public bool NotifyFriends { get; set; } = true;
        public bool NotifySound { get; set; } = true;
    }
}
