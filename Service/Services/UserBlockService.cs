using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly IUserBlockRepository _blocks;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<BlockUserDto> _validator;
        private readonly IMapper _mapper;

        public UserBlockService(IUserBlockRepository blocks,
                            IFriendRequestRepository friendRequests,
                            IAccountRepository accounts,
                            IValidator<BlockUserDto> validator,
                            IMapper mapper)
        {
            _blocks = blocks;
            _friendRequests = friendRequests;
            _accounts = accounts;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<FriendRequestDto> BlockAsync(string blockerId, BlockUserDto model)
        {
            await _validator.EnsureValidAsync(model);

            AppUser? target = null;
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                target = await _accounts.GetByIdAsync(model.UserId.Trim());
            }

            if (target is null && !string.IsNullOrWhiteSpace(model.UserName))
            {
                target = await _accounts.GetByUserNameAsync(model.UserName.Trim());
            }

            if (target is null)
            {
                throw new NotFoundException("No user was found with that username.");
            }

            if (target.Id == blockerId)
            {
                throw new BadRequestException("You cannot block yourself.");
            }

            if (await _blocks.GetBlockAsync(target.Id, blockerId) is not null)
            {
                throw new BadRequestException("You cannot block this user.");
            }

            var block = await _blocks.GetBlockAsync(blockerId, target.Id);
            if (block is null)
            {
                block = new UserBlock
                {
                    BlockerId = blockerId,
                    BlockedId = target.Id
                };
                await _blocks.AddAsync(block);
            }

            var friendship = await _friendRequests.GetBetweenAsync(blockerId, target.Id);
            if (friendship is not null)
            {
                await _friendRequests.DeleteAsync(friendship);
            }

            return MapBlocked(block.Id, target);
        }

        public async Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId)
        {
            var rows = await _blocks.GetBlockedByUserAsync(userId);
            return rows.Select(x => MapBlocked(x.Id, x.Blocked)).ToList();
        }

        public async Task UnblockAsync(string userId, int blockId)
        {
            var block = await _blocks.GetByIdAsync(blockId);
            if (block is null || block.BlockerId != userId)
            {
                throw new NotFoundException("Blocked user not found.");
            }

            await _blocks.DeleteAsync(block);
        }

        private FriendRequestDto MapBlocked(int id, AppUser user)
        {
            var dto = _mapper.Map<FriendRequestDto>(user);
            dto.Id = id;
            return dto;
        }
    }
}
