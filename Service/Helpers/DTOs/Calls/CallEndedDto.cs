namespace Service.Helpers.DTOs.Calls
{
    public class CallEndedDto
    {
        public string CallId { get; set; } = string.Empty;
        public string Reason { get; set; } = "ended";
        public string CallerId { get; set; } = string.Empty;
        public string CalleeId { get; set; } = string.Empty;
        public Service.Helpers.DTOs.DirectMessages.EditedMessageDto? Message { get; set; }
    }
}
