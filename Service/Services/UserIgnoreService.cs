using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class UserIgnoreService : IUserIgnoreService
    {
        private readonly IUserIgnoreRepository _ignores;
        private readonly IAccountRepository _accounts;

        public UserIgnoreService(IUserIgnoreRepository ignores, IAccountRepository accounts)
        {
            _ignores = ignores;
            _accounts = accounts;
        }

        public async Task<IgnoreDto> ToggleAsync(string userId, IgnoreUserDto model)
        {
            var target = string.IsNullOrWhiteSpace(model.UserName) ? null : await _accounts.GetByUserNameAsync(model.UserName.Trim());
            if (target is null)
            {
                throw new NotFoundException("No user was found with that username.");
            }

            if (target.Id == userId)
            {
                throw new BadRequestException("You cannot ignore yourself.");
            }

            var existing = await _ignores.GetAsync(userId, target.Id);
            if (existing is null)
            {
                await _ignores.AddAsync(new UserIgnore { IgnorerId = userId, IgnoredId = target.Id });
            }
            else
            {
                await _ignores.DeleteAsync(existing);
            }

            return new IgnoreDto { UserName = target.UserName ?? string.Empty, Ignored = existing is null };
        }

        public async Task<IEnumerable<string>> GetIgnoredUserNamesAsync(string userId)
        {
            var ignored = await _ignores.GetIgnoredByUserAsync(userId);
            return ignored.Select(x => x.Ignored.UserName ?? string.Empty).Where(x => x.Length > 0).ToList();
        }
    }
}
