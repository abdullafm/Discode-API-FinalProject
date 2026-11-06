namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerOverwriteInputDto
    {
        public int? RoleId { get; set; }
        public string? UserId { get; set; }
        public long Allow { get; set; }
        public long Deny { get; set; }
    }
}
