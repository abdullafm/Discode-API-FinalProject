using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _dbContext;

        public AdminRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> CountUsersAsync()
        {
            return _dbContext.Users.CountAsync();
        }

        public Task<int> CountServersAsync()
        {
            return _dbContext.Servers.CountAsync();
        }

        public Task<int> CountOpenReportsAsync()
        {
            return _dbContext.Reports.CountAsync(x => x.Status == ReportStatus.Open);
        }

        public Task<int> CountPostsAsync()
        {
            return _dbContext.Posts.CountAsync();
        }
    }
}
