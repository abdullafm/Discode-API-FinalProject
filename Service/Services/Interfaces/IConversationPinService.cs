namespace Service.Services.Interfaces
{
    public interface IConversationPinService
    {
        Task<List<string>> GetAsync(string userId);
        // pins the person, or unpins them when they were pinned; returns the new list
        Task<List<string>> ToggleAsync(string userId, string key);
    }
}
