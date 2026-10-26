namespace Service.Helpers.DTOs.Accounts
{
    public class UpdateProfileDto
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public string Pronouns { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }
}
