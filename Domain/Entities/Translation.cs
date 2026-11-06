using Domain.Common;

namespace Domain.Entities
{
    public class Translation : BaseEntity
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
