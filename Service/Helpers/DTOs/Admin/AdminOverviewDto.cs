namespace Service.Helpers.DTOs.Admin
{
    public class AdminOverviewDto
    {
        public AdminUserStatsDto Users { get; set; } = new();
        public AdminServerStatsDto Servers { get; set; } = new();
        public AdminBanStatsDto Bans { get; set; } = new();
        public AdminContentStatsDto Content { get; set; } = new();
        public AdminReportStatsDto Reports { get; set; } = new();
    }

    public class AdminUserStatsDto
    {
        public int Total { get; set; }
        public int NewLast7Days { get; set; }
        public int NewLast30Days { get; set; }
        public int PublicAccounts { get; set; }
        public int PrivateAccounts { get; set; }
        public int Admins { get; set; }
        public int SuperAdmins { get; set; }
    }

    public class AdminServerStatsDto
    {
        public int Total { get; set; }
        public int Public { get; set; }
        public int Private { get; set; }
        public int Banned { get; set; }
    }

    public class AdminBanStatsDto
    {
        public int TotalBans { get; set; }
        public int BansLast7Days { get; set; }
        public int UsersBanned { get; set; }
        public int ServersWithBans { get; set; }
        public int ActiveTimeouts { get; set; }
    }

    public class AdminContentStatsDto
    {
        public int Posts { get; set; }
        public int Comments { get; set; }
    }

    public class AdminReportStatsDto
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int Reviewed { get; set; }
        public int Dismissed { get; set; }
        public List<AdminReportReasonDto> TopReasons { get; set; } = new();
    }

    public class AdminReportReasonDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
