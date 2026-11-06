namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerPermissionSetDto
    {
        public bool CanSync { get; set; }
        public bool Synced { get; set; }
        public int EveryoneRoleId { get; set; }
        public List<ServerOverwriteDto> Overwrites { get; set; } = new();
    }
}
