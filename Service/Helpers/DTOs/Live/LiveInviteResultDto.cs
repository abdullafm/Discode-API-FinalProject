namespace Service.Helpers.DTOs.Live
{
    public class LiveInviteResultDto
    {
        public string Title { get; set; } = string.Empty;
        public List<string> UserNames { get; set; } = new();
    }
}
