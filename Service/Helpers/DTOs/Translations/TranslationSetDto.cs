namespace Service.Helpers.DTOs.Translations
{
    public class TranslationSetDto
    {
        public string Language { get; set; } = "en";
        public Dictionary<string, string> Entries { get; set; } = new();
    }
}
