namespace Repository.Repositories.Interfaces
{
    public interface IPresenceTrackerRepository
    {
        Task<bool> ConnectedAsync(string userId, string connectionId);
        Task<bool> DisconnectedAsync(string userId, string connectionId);
        Task<bool> IsOnlineAsync(string userId);
    }
}
