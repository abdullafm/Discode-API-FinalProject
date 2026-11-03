namespace Domain.Enums
{
    public enum AdminActionKind
    {
        Warn = 0,
        Kick = 1,
        Ban = 2,
        Unban = 3,
        Timeout = 4,
        ClearTimeout = 5,
        Delete = 6,
        ServerBan = 7,
        ServerUnban = 8,
        ServerDelete = 9,
        ServerWarn = 10,
        BrandingChange = 11,
        ChangeRole = 12,
        ReportReviewed = 13,
        ReportDismissed = 14,
        ReportReopened = 15,
        LiveBan = 16,
        LiveUnban = 17,
        LiveEnd = 18,
        ContentDelete = 19
    }
}
