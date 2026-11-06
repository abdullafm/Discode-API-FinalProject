namespace Repository.Repositories.Interfaces
{
    public interface IGroupCallRingRepository
    {
        Task SetAsync(int callId, string userId, DateTime? ringingUntil);
        Task<Dictionary<string, DateTime?>> GetAsync(int callId);
    }
}
