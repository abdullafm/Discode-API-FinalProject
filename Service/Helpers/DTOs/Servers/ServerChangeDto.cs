namespace Service.Helpers.DTOs.Servers
{
    public class ServerChangeDto
    {
        public ServerDto Server { get; set; } = new();
        public List<string> MemberIds { get; set; } = new();
        public bool Joined { get; set; }
    }
}
