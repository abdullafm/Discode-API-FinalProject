namespace Service.Helpers.DTOs.Calls
{
    public class CallDto
    {
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = "ringing";
        public string Role { get; set; } = "caller";
        public bool Video { get; set; }
        public CallPartyDto Other { get; set; } = new();
    }
}
