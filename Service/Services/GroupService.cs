using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
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

        private const int HistoryLimit = 100;

        private readonly IDirectMessageService _directMessages;
        private readonly IGroupRepository _groups;
        private readonly IGroupMessageRepository _messages;
        private readonly IPollRepository _polls;
        private readonly IValidator<CreatePollDto> _pollValidator;
        private readonly IValidator<ToggleReactionDto> _reactionValidator;
        private readonly IAccountRepository _accounts;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IUserBlockRepository _blocks;
        private readonly IValidator<CreateGroupDto> _validator;
        private readonly IValidator<SendGroupMessageDto> _messageValidator;
        private readonly IValidator<UpdateGroupDto> _updateValidator;
        private readonly IValidator<AddGroupMembersDto> _addValidator;
        private readonly IMapper _mapper;

        public GroupService(IDirectMessageService directMessages,
                            IGroupRepository groups,
                            IGroupMessageRepository messages,
                            IPollRepository polls,
                            IValidator<CreatePollDto> pollValidator,
                            IValidator<ToggleReactionDto> reactionValidator,
                            IAccountRepository accounts,
                            IFriendRequestRepository friendRequests,
                            IUserBlockRepository blocks,
                            IValidator<CreateGroupDto> validator,
                            IValidator<SendGroupMessageDto> messageValidator,
                            IValidator<UpdateGroupDto> updateValidator,
                            IValidator<AddGroupMembersDto> addValidator,
                            IMapper mapper)
        {
            _directMessages = directMessages;
            _groups = groups;
            _messages = messages;
            _polls = polls;
            _pollValidator = pollValidator;
            _reactionValidator = reactionValidator;
            _messageValidator = messageValidator;
            _updateValidator = updateValidator;
            _addValidator = addValidator;
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
            var groups = (await _groups.GetForUserAsync(userId)).ToList();
            var activity = await _messages.GetLastActivityAsync(groups.Select(g => g.Id).ToList());
            var dtos = groups.Select(ToDto).ToList();
            var unread = await _groups.GetUnreadCountsAsync(userId);
            foreach (var dto in dtos)
            {
                dto.UnreadCount = unread.TryGetValue(dto.Id, out var count) ? count : 0;
                if (activity.TryGetValue(dto.Id, out var last) && last > dto.LastMessageAt)
                {
                    dto.LastMessageAt = last;
                }
            }

            return dtos;
        }

        public async Task MarkReadAsync(string userId, int groupId)
        {
            await EnsureMemberAsync(userId, groupId);
            await _groups.MarkReadAsync(groupId, userId);
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

        public async Task<IEnumerable<GroupMessageDto>> GetMessagesAsync(string userId, int groupId, int? beforeId = null)
        {
            await EnsureMemberAsync(userId, groupId);
            var messages = await _messages.GetLatestAsync(groupId, HistoryLimit, beforeId);
            return _mapper.Map<List<GroupMessageDto>>(messages, options => options.Items[Service.Helpers.Mappings.MappingProfile.ViewerIdKey] = userId);
        }

        public async Task<GroupChangeDto> SendMessageAsync(string userId, int groupId, SendGroupMessageDto model)
        {
            await _messageValidator.EnsureValidAsync(model);
            var group = await EnsureMemberAsync(userId, groupId);

            if (model.ReplyToMessageId.HasValue)
            {
                var target = await _messages.GetWithSenderAsync(model.ReplyToMessageId.Value);
                if (target is null || target.GroupId != groupId || target.IsSystem)
                {
                    throw new NotFoundException("Message not found.");
                }
            }

            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = userId,
                ReplyToMessageId = model.ReplyToMessageId,
                Text = model.Text?.Trim() ?? string.Empty,
                Attachments = model.Attachments
                    .Select(a => new GroupMessageAttachment { Url = a.Url, FileName = a.FileName, ContentType = a.ContentType, Size = a.Size })
                    .ToList()
            };
            await _messages.AddAsync(message);
            return Change(group, await LoadMessageAsync(message.Id));
        }

        public async Task<GroupChangeDto> SendPollAsync(string userId, int groupId, CreatePollDto model)
        {
            await _pollValidator.EnsureValidAsync(model);
            var group = await EnsureMemberAsync(userId, groupId);

            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = userId,
                Text = string.Empty,
                Poll = new Poll
                {
                    Question = model.Question.Trim(),
                    AllowMultiple = model.AllowMultiple,
                    ExpiresAt = DateTime.UtcNow.AddHours(model.DurationHours),
                    Options = model.Options
                        .Select((o, index) => new PollOption { Text = o.Text.Trim(), Emoji = (o.Emoji ?? string.Empty).Trim(), Position = index })
                        .ToList()
                }
            };
            await _messages.AddAsync(message);
            return Change(group, await LoadMessageAsync(message.Id));
        }

        public async Task<GroupMessageDeletedDto> DeleteMessageAsync(string userId, int groupId, int messageId)
        {
            var group = await EnsureMemberAsync(userId, groupId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.GroupId != groupId || message.IsSystem || message.Call is not null || message.SenderId != userId)
            {
                throw new NotFoundException("Message not found.");
            }

            await _messages.ClearRepliesAsync(messageId);
            await _messages.DeleteAsync(await _messages.GetByIdAsync(messageId) ?? throw new NotFoundException("Message not found."));
            return new GroupMessageDeletedDto { GroupId = groupId, MessageId = messageId, UserIds = group.Members.Select(m => m.UserId).ToList() };
        }

        public async Task<IEnumerable<SentMessageDto>> ForwardMessageAsync(string userId, int groupId, int messageId, ForwardMessageDto model)
        {
            await EnsureMemberAsync(userId, groupId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.GroupId != groupId || message.IsSystem || message.Call is not null || message.Poll is not null)
            {
                throw new NotFoundException("Message not found.");
            }

            var attachments = message.Attachments
                .Select(a => new MessageAttachmentDto { Url = a.Url, FileName = a.FileName, ContentType = a.ContentType, Size = a.Size })
                .ToList();
            return await _directMessages.ForwardContentAsync(userId, message.SenderId, message.Text, attachments, model);
        }

        public async Task<GroupMessageUpdateDto> ToggleReactionAsync(string userId, int groupId, int messageId, ToggleReactionDto model)
        {
            await _reactionValidator.EnsureValidAsync(model);
            var group = await EnsureMemberAsync(userId, groupId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.GroupId != groupId || message.IsSystem || message.Call is not null)
            {
                throw new NotFoundException("Message not found.");
            }

            await _messages.ToggleReactionAsync(messageId, userId, model.Emoji.Trim());
            return await ViewsAsync(group, messageId);
        }

        public async Task<GroupMessageUpdateDto> VotePollAsync(string userId, int groupId, int messageId, VotePollDto model)
        {
            var group = await EnsureMemberAsync(userId, groupId);
            var message = await _messages.GetWithSenderAsync(messageId);
            if (message is null || message.GroupId != groupId)
            {
                throw new NotFoundException("Poll not found.");
            }

            var poll = await _polls.GetByGroupMessageIdAsync(messageId);
            if (poll is null)
            {
                throw new NotFoundException("Poll not found.");
            }

            if (poll.ExpiresAt <= DateTime.UtcNow)
            {
                throw new BadRequestException("This poll has ended.");
            }

            var optionIds = (model.OptionIds ?? new List<int>()).Distinct().ToList();
            if (optionIds.Any(id => poll.Options.All(o => o.Id != id)))
            {
                throw new BadRequestException("Choose one of the answers.");
            }

            if (!poll.AllowMultiple && optionIds.Count > 1)
            {
                throw new BadRequestException("You can choose only one answer.");
            }

            await _polls.SetVotesAsync(poll, userId, optionIds);
            return await ViewsAsync(group, messageId);
        }

        private async Task<GroupMessageUpdateDto> ViewsAsync(Group group, int messageId)
        {
            var saved = await _messages.GetWithSenderAsync(messageId);
            var update = new GroupMessageUpdateDto { GroupId = group.Id };
            foreach (var member in group.Members)
            {
                update.Views[member.UserId] = _mapper.Map<GroupMessageDto>(saved, options => options.Items[Service.Helpers.Mappings.MappingProfile.ViewerIdKey] = member.UserId);
            }

            return update;
        }

        public async Task<GroupChangeDto> UpdateAsync(string userId, int groupId, UpdateGroupDto model)
        {
            await _updateValidator.EnsureValidAsync(model);
            await EnsureMemberAsync(userId, groupId);

            var group = (await _groups.GetTrackedWithMembersAsync(groupId))!;
            var actor = DisplayNameOf(group.Members.First(m => m.UserId == userId).User);
            var name = model.Name.Trim();
            var icon = model.IconUrl?.Trim() ?? string.Empty;
            var notes = new List<string>();
            if (!string.Equals(group.Name, name, StringComparison.Ordinal))
            {
                notes.Add(actor + " changed the group name to " + name + ".");
            }

            if (!string.Equals(group.IconUrl, icon, StringComparison.Ordinal))
            {
                notes.Add(actor + " changed the group icon.");
            }

            group.Name = name;
            group.IconUrl = icon;
            await _groups.UpdateAsync(group);

            GroupMessageDto? systemMessage = null;
            foreach (var note in notes)
            {
                systemMessage = await AddSystemMessageAsync(groupId, userId, note);
            }

            return Change(group, systemMessage);
        }

        public async Task<GroupChangeDto> AddMembersAsync(string userId, int groupId, AddGroupMembersDto model)
        {
            await _addValidator.EnsureValidAsync(model);
            var group = await EnsureMemberAsync(userId, groupId);

            var added = new List<AppUser>();
            foreach (var userName in model.UserNames.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var friend = await _accounts.GetByUserNameAsync(userName);
                if (friend is null || friend.Id == userId)
                {
                    throw new NotFoundException("User not found.");
                }

                if (group.Members.Any(m => m.UserId == friend.Id))
                {
                    throw new BadRequestException(userName + " is already in this group.");
                }

                if (await _blocks.IsBlockedEitherWayAsync(userId, friend.Id))
                {
                    throw new BadRequestException("You cannot add " + userName + " to a group.");
                }

                var friendship = await _friendRequests.GetBetweenAsync(userId, friend.Id);
                if (friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("You can only add your friends to a group. " + userName + " is not your friend.");
                }

                added.Add(friend);
            }

            if (group.Members.Count + added.Count > CreateGroupDtoValidator.MaxMembers)
            {
                throw new BadRequestException("A group can have up to " + CreateGroupDtoValidator.MaxMembers + " members.");
            }

            await _groups.AddMembersAsync(groupId, added.Select(a => a.Id));
            var actor = DisplayNameOf(group.Members.First(m => m.UserId == userId).User);
            var note = await AddSystemMessageAsync(groupId, userId, actor + " added " + string.Join(", ", added.Select(DisplayNameOf)) + " to the group.");
            var fresh = (await _groups.GetWithMembersAsync(groupId))!;
            return Change(fresh, note);
        }

        public async Task<GroupChangeDto> RemoveMemberAsync(string userId, int groupId, string userName)
        {
            var group = await EnsureMemberAsync(userId, groupId);
            if (group.OwnerId != userId)
            {
                throw new BadRequestException("Only the group owner can remove people.");
            }

            var target = group.Members.FirstOrDefault(m => string.Equals(m.User.UserName, (userName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                throw new NotFoundException("That person is not in this group.");
            }

            if (target.UserId == userId)
            {
                throw new BadRequestException("Use Leave Group to leave.");
            }

            var affected = group.Members.Select(m => m.UserId).ToList();
            var actor = DisplayNameOf(group.Members.First(m => m.UserId == userId).User);
            await _groups.RemoveMemberAsync(groupId, target.UserId);
            var note = await AddSystemMessageAsync(groupId, userId, actor + " removed " + DisplayNameOf(target.User) + " from the group.");
            var fresh = (await _groups.GetWithMembersAsync(groupId))!;
            var change = Change(fresh, note);
            change.UserIds = affected;
            return change;
        }

        public async Task<GroupChangeDto> LeaveAsync(string userId, int groupId)
        {
            var group = await EnsureMemberAsync(userId, groupId);
            var affected = group.Members.Select(m => m.UserId).ToList();
            var leaver = DisplayNameOf(group.Members.First(m => m.UserId == userId).User);

            if (group.Members.Count == 1)
            {
                await _groups.DeleteAsync((await _groups.GetByIdAsync(groupId))!);
                return new GroupChangeDto { GroupId = groupId, UserIds = affected };
            }

            await _groups.RemoveMemberAsync(groupId, userId);
            if (group.OwnerId == userId)
            {
                var tracked = (await _groups.GetTrackedWithMembersAsync(groupId))!;
                tracked.OwnerId = tracked.Members.OrderBy(m => m.Id).First().UserId;
                tracked.Owner = null!;
                await _groups.UpdateAsync(tracked);
            }

            var remaining = group.Members.First(m => m.UserId != userId).UserId;
            var note = await AddSystemMessageAsync(groupId, remaining, leaver + " left the group.");
            var fresh = (await _groups.GetWithMembersAsync(groupId))!;
            var change = Change(fresh, note);
            change.UserIds = affected;
            return change;
        }

        private async Task<Group> EnsureMemberAsync(string userId, int groupId)
        {
            var group = await _groups.GetWithMembersAsync(groupId);
            if (group is null || group.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Group not found.");
            }

            return group;
        }

        private async Task<GroupMessageDto> AddSystemMessageAsync(int groupId, string actorId, string text)
        {
            var message = new GroupMessage { GroupId = groupId, SenderId = actorId, Text = text.Length > 2000 ? text.Substring(0, 2000) : text, IsSystem = true };
            await _messages.AddAsync(message);
            return await LoadMessageAsync(message.Id);
        }

        private async Task<GroupMessageDto> LoadMessageAsync(int id)
        {
            var saved = await _messages.GetWithSenderAsync(id);
            return _mapper.Map<GroupMessageDto>(saved, options => options.Items[Service.Helpers.Mappings.MappingProfile.ViewerIdKey] = string.Empty);
        }

        private GroupChangeDto Change(Group group, GroupMessageDto? message)
        {
            return new GroupChangeDto
            {
                GroupId = group.Id,
                Group = ToDto(group),
                UserIds = group.Members.Select(m => m.UserId).ToList(),
                Message = message
            };
        }

        private GroupDto ToDto(Group group)
        {
            var dto = _mapper.Map<GroupDto>(group);
            dto.MaxMembers = CreateGroupDtoValidator.MaxMembers;
            dto.LastMessageAt = group.CreatedAt;
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
