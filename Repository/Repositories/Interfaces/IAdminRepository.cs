namespace Repository.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        Task<int> CountUsersAsync();
        Task<int> CountServersAsync();
        Task<int> CountOpenReportsAsync();
        Task<int> CountPostsAsync();
    }
}
