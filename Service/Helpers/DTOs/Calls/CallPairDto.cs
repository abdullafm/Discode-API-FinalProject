namespace Service.Helpers.DTOs.Calls
{
    public class CallPairDto
    {
        public string CallerId { get; set; } = string.Empty;
        public string CalleeId { get; set; } = string.Empty;
        public CallDto ForCaller { get; set; } = new();
        public CallDto ForCallee { get; set; } = new();
    }
}
