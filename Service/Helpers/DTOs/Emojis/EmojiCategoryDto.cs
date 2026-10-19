namespace Service.Helpers.DTOs.Emojis
{
    public class EmojiCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<EmojiDto> Emojis { get; set; } = new();
    }
}
