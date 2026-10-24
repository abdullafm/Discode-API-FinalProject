namespace Service.Helpers.DTOs.Groups
{
    public class GroupMessageUpdateDto
    {
        public int GroupId { get; set; }
        public Dictionary<string, GroupMessageDto> Views { get; set; } = new();
    }
}
