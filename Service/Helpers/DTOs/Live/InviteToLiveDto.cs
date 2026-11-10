namespace Service.Helpers.DTOs.Live
{
    public class InviteToLiveDto
    {
        public List<string> UserNames { get; set; } = new();
        public List<int> GroupIds { get; set; } = new();
    }
}
