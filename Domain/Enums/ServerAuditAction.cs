namespace Domain.Enums
{
    public enum ServerAuditAction
    {
        ServerUpdate = 1,
        InviteUpdate = 2,
        ChannelCreate = 3,
        ChannelUpdate = 4,
        ChannelDelete = 5,
        CategoryCreate = 6,
        CategoryUpdate = 7,
        CategoryDelete = 8,
        RoleCreate = 9,
        RoleUpdate = 10,
        RoleDelete = 11,
        MemberJoin = 12,
        MemberLeave = 13,
        MemberKick = 14,
        MemberBan = 15,
        MemberUnban = 16,
        MemberRoleUpdate = 17,
        MemberMute = 18,
        MemberUnmute = 19,
        MemberVoiceDisconnect = 20,
        MemberTimeout = 21,
        MemberTimeoutRemove = 22
    }
}
