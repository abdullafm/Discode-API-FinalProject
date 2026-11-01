namespace Service.Helpers.DTOs.Admin
{
    public class ModerationActor
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
    }

    public class WarnUserDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class TimeoutUserDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Minutes { get; set; }
    }

    public class KickUserDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class BanUserDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Days { get; set; }
    }

    public class DeleteUserDto
    {
        public string Reason { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class ModerationResultDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool ForceLogout { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int WarningId { get; set; }
        public int WarningCount { get; set; }
        public DateTime? TimeoutUntil { get; set; }
    }

    public class UserWarningDto
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string IssuedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Seen { get; set; }
    }

    public class UnseenWarningDto
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminActionDto
    {
        public int Id { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminActionListDto
    {
        public List<AdminActionDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminUserDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsDeleted { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? BannedUntil { get; set; }
        public DateTime? BannedAt { get; set; }
        public string BanReason { get; set; } = string.Empty;
        public string BannedByUserName { get; set; } = string.Empty;
        public bool TimedOut { get; set; }
        public DateTime? TimeoutUntil { get; set; }
        public string TimeoutReason { get; set; } = string.Empty;
        public int ServersJoined { get; set; }
        public int BansReceived { get; set; }
        public int Warnings { get; set; }
        public int ReportsAgainst { get; set; }
        public int Posts { get; set; }
        public int Comments { get; set; }
        public bool CanModerate { get; set; }
        public bool CanDelete { get; set; }
        public List<UserWarningDto> RecentWarnings { get; set; } = new();
        public List<AdminActionDto> History { get; set; } = new();
    }
}
