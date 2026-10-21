namespace Service.Helpers.DTOs.DirectMessages
{
    public class ForwardMessageDto
    {
        public List<string> UserNames { get; set; } = new();
        public string Note { get; set; } = string.Empty;
    }
}
