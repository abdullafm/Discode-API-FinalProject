namespace Service.Services.Interfaces
{
    public interface IOwoService
    {
        Task<string?> PlayAsync(int serverId, string userId, string text);
        Task<string?> GetBotIdAsync();
        Task SetEnabledAsync(string userId, int serverId, bool enabled);
    }
}
