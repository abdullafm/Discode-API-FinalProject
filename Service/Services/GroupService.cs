using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Helpers.Validators.Groups;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class GroupService : IGroupService
    {
        private const int MaxNameLength = 100;

        private readonly IGroupRepository _groups;
        private readonly IAccountRepository _accounts;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IUserBlockRepository _blocks;
        private readonly IValidator<CreateGroupDto> _validator;
        private readonly IMapper _mapper;

        public GroupService(IGroupRepository groups,
                            IAccountRepository accounts,
                            IFriendRequestRepository friendRequests,
                            IUserBlockRepository blocks,
                            IValidator<CreateGroupDto> validator,
                            IMapper mapper)
        {
            _groups = groups;
            _accounts = accounts;
            _friendRequests = friendRequests;
            _blocks = blocks;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<GroupDto> CreateAsync(string ownerId, CreateGroupDto model)
        {
            await _validator.EnsureValidAsync(model);

            var owner = await _accounts.GetByIdAsync(ownerId);
            if (owner is null)
            {
                throw new NotFoundException("User not found.");
            }

            var members = new List<AppUser> { owner };
            var userNames = model.UserNames.Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var userName in userNames)
            {
                var friend = await _accounts.GetByUserNameAsync(userName);
                if (friend is null || friend.Id == ownerId)
                {
                    throw new NotFoundException("User not found.");
                }

                if (await _blocks.IsBlockedEitherWayAsync(ownerId, friend.Id))
                {
                    throw new BadRequestException("You cannot add " + userName + " to a group.");
                }

                var friendship = await _friendRequests.GetBetweenAsync(ownerId, friend.Id);
                if (friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("You can only add your friends to a group. " + userName + " is not your friend.");
                }

                members.Add(friend);
            }

            if (members.Count < 3)
            {
                throw new BadRequestException("Pick at least two friends for a group.");
            }

            if (members.Count > CreateGroupDtoValidator.MaxMembers)
            {
                throw new BadRequestException("A group can have up to " + CreateGroupDtoValidator.MaxMembers + " members including you.");
            }

            var existing = await _groups.FindWithSameMembersAsync(members.Select(m => m.Id).ToList());
            if (existing is not null && existing.OwnerId == ownerId)
            {
                var found = ToDto(existing);
                found.Existing = true;
                return found;
            }

            var name = model.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                name = string.Join(", ", members.Skip(1).Select(DisplayNameOf));
                if (name.Length > MaxNameLength)
                {
                    name = name.Substring(0, MaxNameLength);
                }
            }

            var group = new Group
            {
                Name = name,
                OwnerId = ownerId,
                Members = members.Select(m => new GroupMember { UserId = m.Id }).ToList()
            };
            await _groups.AddAsync(group);

            var saved = await _groups.GetWithMembersAsync(group.Id);
            return ToDto(saved!);
        }

        public async Task<IEnumerable<GroupDto>> GetMineAsync(string userId)
        {
            var groups = await _groups.GetForUserAsync(userId);
            return groups.Select(ToDto).ToList();
        }

        public async Task<GroupDto> GetAsync(string userId, int groupId)
        {
            var group = await _groups.GetWithMembersAsync(groupId);
            if (group is null || group.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Group not found.");
            }

            return ToDto(group);
        }

        private GroupDto ToDto(Group group)
        {
            var dto = _mapper.Map<GroupDto>(group);
            dto.MaxMembers = CreateGroupDtoValidator.MaxMembers;
            dto.MemberCount = group.Members.Count;
            dto.OwnerUserName = group.Members.Where(m => m.UserId == group.OwnerId).Select(m => m.User.UserName ?? string.Empty).FirstOrDefault() ?? string.Empty;
            foreach (var member in dto.Members)
            {
                member.IsOwner = member.UserId == group.OwnerId;
            }

            dto.Members = dto.Members.OrderByDescending(m => m.IsOwner).ThenBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            return dto;
        }

        private static string DisplayNameOf(AppUser user)
        {
            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName;
        }
    }
}
