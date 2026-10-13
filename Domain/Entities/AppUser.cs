using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
