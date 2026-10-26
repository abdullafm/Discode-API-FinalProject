namespace Service.Helpers.DTOs.ServerRoles
{
    public class UpdateServerRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "gray";
        public long Permissions { get; set; }
        public bool DisplaySeparately { get; set; }
    }
}
