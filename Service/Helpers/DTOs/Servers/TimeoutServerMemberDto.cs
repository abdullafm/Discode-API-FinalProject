namespace Service.Helpers.DTOs.Servers
{
    public class TimeoutServerMemberDto
    {
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public string? Reason { get; set; }
    }
}
