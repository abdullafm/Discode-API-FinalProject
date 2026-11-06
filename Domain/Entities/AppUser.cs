using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string PublicId { get; set; } = Domain.Common.PublicIdGenerator.New();
        public string DisplayName { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public string Language { get; set; } = "en";
        // "light" or "dark"
        public string Theme { get; set; } = "light";
        public bool NotifyAll { get; set; } = true;
        public bool NotifyDm { get; set; } = true;
        public bool NotifyFriends { get; set; } = true;
        public bool NotifySound { get; set; } = true;
        public bool IsBanned { get; set; }
        public DateTime? BannedUntil { get; set; }
        public DateTime? BannedAt { get; set; }
        public string BanReason { get; set; } = string.Empty;
        public string? BannedById { get; set; }
        public DateTime? TimeoutUntil { get; set; }
        public string TimeoutReason { get; set; } = string.Empty;
        public DateTime? ForcedLogoutAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSystem { get; set; }
        public bool LiveBanned { get; set; }
        public DateTime? LiveBannedUntil { get; set; }
        public string LiveBanReason { get; set; } = string.Empty;
        public PresenceStatus PreferredStatus { get; set; } = PresenceStatus.Online;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<FriendRequest> SentRequests { get; set; } = new List<FriendRequest>();
        public ICollection<FriendRequest> ReceivedRequests { get; set; } = new List<FriendRequest>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
        public ICollection<UserBlock> BlockedByUsers { get; set; } = new List<UserBlock>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
