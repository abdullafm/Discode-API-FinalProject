namespace Service.Services.Interfaces
{
    public interface IConversationPinService
    {
        Task<List<string>> GetAsync(string userId);
        Task<List<string>> ToggleAsync(string userId, string key);
    }
}
