namespace Service.Helpers.DTOs.ServerChannels
{
    public class UpdateServerChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public bool? SyncWithCategory { get; set; }
        public int? UserLimit { get; set; }
        public List<ServerOverwriteInputDto>? Overwrites { get; set; }
    }
}
