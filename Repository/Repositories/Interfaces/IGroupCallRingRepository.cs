namespace Repository.Repositories.Interfaces
{
    public interface IGroupCallRingRepository
    {
        Task RingAsync(int callId, string userId, TimeSpan window);
        Task<bool> StopAsync(int callId, string userId);
        Task<IEnumerable<string>> GetRingingAsync(int callId);
    }
}
