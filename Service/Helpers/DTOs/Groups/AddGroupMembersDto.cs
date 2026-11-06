namespace Service.Helpers.DTOs.Groups
{
    public class AddGroupMembersDto
    {
        public List<string> UserNames { get; set; } = new();
    }
}
