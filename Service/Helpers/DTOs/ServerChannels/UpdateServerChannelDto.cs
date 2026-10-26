namespace Service.Helpers.DTOs.ServerChannels
{
    public class UpdateServerChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public bool IsReadOnly { get; set; }
        public List<int>? AccessRoleIds { get; set; }
        public List<string>? AccessUserIds { get; set; }
    }
}
