namespace Service.Helpers.DTOs.ServerRoles
{
    public class ServerRoleRefDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "gray";
        public bool DisplaySeparately { get; set; }
    }
}
