namespace Domain.Enums
{
    [Flags]
    public enum ServerPermission : long
    {
        None = 0,
        ViewChannels = 1L << 0,
        ManageChannels = 1L << 1,
        ManageRoles = 1L << 2,
        CreateExpressions = 1L << 3,
        ManageExpressions = 1L << 4,
        ViewAuditLog = 1L << 5,
        ManageWebhooks = 1L << 6,
        ManageServer = 1L << 7,
        SendMessages = 1L << 8,
        ManageMessages = 1L << 9,
        MentionEveryone = 1L << 10,
        Connect = 1L << 11,
        Speak = 1L << 12,
        MuteMembers = 1L << 13,
        KickMembers = 1L << 14,
        BanMembers = 1L << 15,
        All = (1L << 16) - 1
    }
}
