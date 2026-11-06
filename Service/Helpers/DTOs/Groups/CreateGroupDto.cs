namespace Service.Helpers.DTOs.Groups
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> UserNames { get; set; } = new();
    }
}
