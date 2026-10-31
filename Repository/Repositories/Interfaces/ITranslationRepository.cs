namespace Repository.Repositories.Interfaces
{
    public interface ITranslationRepository
    {
        Task<Dictionary<string, string>> GetByLanguageAsync(string languageCode);
        Task<string?> GetUserLanguageAsync(string userId);
        Task SetUserLanguageAsync(string userId, string languageCode);
    }
}
