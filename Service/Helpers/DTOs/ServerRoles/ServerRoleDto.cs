namespace Service.Helpers.DTOs.ServerRoles
{
    public class ServerRoleDto
    {
        public int Id { get; set; }
        public int ServerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "gray";
        public long Permissions { get; set; }
        public int Position { get; set; }
        public bool IsEveryone { get; set; }
        public bool DisplaySeparately { get; set; }
        public int MemberCount { get; set; }
    }
}
