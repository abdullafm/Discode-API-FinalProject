using AutoMapper;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class GroupCallService : IGroupCallService
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly IGroupCallRepository _calls;
        private readonly IGroupRepository _groups;
        private readonly IGroupMessageRepository _messages;
        private readonly IMapper _mapper;

        public GroupCallService(IGroupCallRepository calls, IGroupRepository groups, IGroupMessageRepository messages, IMapper mapper)
        {
            _calls = calls;
            _groups = groups;
            _messages = messages;
            _mapper = mapper;
        }

        public async Task<GroupCallChangeDto> StartAsync(string userId, int groupId, bool video)
        {
            var group = await _groups.GetWithMembersAsync(groupId);
            if (group is null || group.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Group not found.");
            }

            await Gate.WaitAsync();
            try
            {
                var left = await LeaveOthersAsync(userId, null, groupId);
                var existing = await _calls.GetActiveForGroupAsync(groupId);
                if (existing is not null)
                {
                    await _calls.SetParticipantAsync(existing.Id, userId, true);
                    var joined = await BuildAsync(existing.Id, group, false, false, null);
                    joined.Left = left;
                    return joined;
                }

                var message = new GroupMessage { GroupId = groupId, SenderId = userId, Text = string.Empty };
                await _messages.AddAsync(message);
                var call = new GroupCall
                {
                    GroupId = groupId,
                    StarterId = userId,
                    Video = video,
                    GroupMessageId = message.Id,
                    Participants = new List<GroupCallParticipant> { new GroupCallParticipant { UserId = userId, JoinedAt = DateTime.UtcNow } }
                };
                await _calls.AddAsync(call);

                var change = await BuildAsync(call.Id, group, true, false, await LoadMessageAsync(message.Id));
                change.Left = left;
                return change;
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<GroupCallChangeDto> JoinAsync(string userId, int callId)
        {
            await Gate.WaitAsync();
            try
            {
                var call = await _calls.GetFullAsync(callId);
                if (call is null || !call.Active)
                {
                    throw new NotFoundException("This call has ended.");
                }

                var group = await _groups.GetWithMembersAsync(call.GroupId);
                if (group is null || group.Members.All(m => m.UserId != userId))
                {
                    throw new NotFoundException("Group not found.");
                }

                var left = await LeaveOthersAsync(userId, callId, null);
                await _calls.SetParticipantAsync(callId, userId, true);
                var change = await BuildAsync(callId, group, false, false, null);
                change.Left = left;
                return change;
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<GroupCallChangeDto?> LeaveAsync(string userId, int callId)
        {
            await Gate.WaitAsync();
            try
            {
                return await LeaveInternalAsync(userId, callId);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<IEnumerable<GroupCallChangeDto>> LeaveAllForAsync(string userId)
        {
            await Gate.WaitAsync();
            try
            {
                return await LeaveOthersAsync(userId, null, null);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<GroupCallChangeDto?> SetScreenAsync(string userId, int callId, bool on)
        {
            await Gate.WaitAsync();
            try
            {
                var call = await _calls.GetFullAsync(callId);
                if (call is null || !call.Active || !call.Participants.Any(p => p.UserId == userId && p.LeftAt == null))
                {
                    return null;
                }

                await _calls.SetScreenAsync(callId, userId, on);
                var group = await _groups.GetWithMembersAsync(call.GroupId);
                return group is null ? null : await BuildAsync(callId, group, false, false, null);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<GroupCallDto?> GetMineAsync(string userId)
        {
            var call = (await _calls.GetActiveForUserAsync(userId)).FirstOrDefault();
            return call is null ? null : ToDto(call);
        }

        public async Task<IEnumerable<GroupCallDto>> GetActiveInMyGroupsAsync(string userId)
        {
            var groups = await _groups.GetForUserAsync(userId);
            var ids = groups.Select(g => g.Id).ToList();
            var active = await _calls.GetActiveForGroupsAsync(ids);
            return active.Select(ToDto).ToList();
        }

        public async Task<bool> AreTogetherAsync(string userId, string otherUserId, int callId)
        {
            var call = await _calls.GetFullAsync(callId);
            if (call is null || !call.Active)
            {
                return false;
            }

            return call.Participants.Any(p => p.UserId == userId && p.LeftAt == null) &&
                   call.Participants.Any(p => p.UserId == otherUserId && p.LeftAt == null);
        }

        private async Task<List<GroupCallChangeDto>> LeaveOthersAsync(string userId, int? keepCallId, int? keepGroupId)
        {
            var result = new List<GroupCallChangeDto>();
            foreach (var call in await _calls.GetActiveForUserAsync(userId))
            {
                if (call.Id == keepCallId || call.GroupId == keepGroupId)
                {
                    continue;
                }

                var change = await LeaveInternalAsync(userId, call.Id);
                if (change is not null)
                {
                    result.Add(change);
                }
            }

            return result;
        }

        private async Task<GroupCallChangeDto?> LeaveInternalAsync(string userId, int callId)
        {
            var call = await _calls.GetFullAsync(callId);
            if (call is null || !call.Active || !call.Participants.Any(p => p.UserId == userId && p.LeftAt == null))
            {
                return null;
            }

            await _calls.SetParticipantAsync(callId, userId, false);
            var group = await _groups.GetWithMembersAsync(call.GroupId);
            if (group is null)
            {
                return null;
            }

            var remaining = call.Participants.Count(p => p.LeftAt == null && p.UserId != userId);
            if (remaining == 0)
            {
                await _calls.EndAsync(callId);
                return await BuildAsync(callId, group, false, true, await LoadMessageAsync(call.GroupMessageId));
            }

            return await BuildAsync(callId, group, false, false, null);
        }

        private async Task<GroupCallChangeDto> BuildAsync(int callId, Group group, bool started, bool ended, GroupMessageDto? message)
        {
            var call = (await _calls.GetFullAsync(callId))!;
            return new GroupCallChangeDto
            {
                Call = ToDto(call),
                MemberIds = group.Members.Select(m => m.UserId).ToList(),
                Started = started,
                Ended = ended,
                Message = message
            };
        }

        private async Task<GroupMessageDto> LoadMessageAsync(int id)
        {
            var saved = await _messages.GetWithSenderAsync(id);
            return _mapper.Map<GroupMessageDto>(saved, options => options.Items[Service.Helpers.Mappings.MappingProfile.ViewerIdKey] = string.Empty);
        }

        private static GroupCallDto ToDto(GroupCall call)
        {
            return new GroupCallDto
            {
                CallId = call.Id,
                GroupId = call.GroupId,
                GroupName = call.Group.Name,
                GroupIconUrl = call.Group.IconUrl ?? string.Empty,
                MessageId = call.GroupMessageId,
                Video = call.Video,
                Active = call.Active,
                StartedAt = call.CreatedAt,
                StarterUserName = call.Starter.UserName ?? string.Empty,
                StarterDisplayName = string.IsNullOrWhiteSpace(call.Starter.DisplayName) ? call.Starter.UserName ?? string.Empty : call.Starter.DisplayName,
                StarterAvatarUrl = call.Starter.AvatarUrl ?? string.Empty,
                Participants = call.Participants
                    .Where(p => p.LeftAt == null)
                    .OrderBy(p => p.JoinedAt)
                    .Select(p => new GroupCallParticipantDto
                    {
                        UserId = p.UserId,
                        UserName = p.User.UserName ?? string.Empty,
                        DisplayName = string.IsNullOrWhiteSpace(p.User.DisplayName) ? p.User.UserName ?? string.Empty : p.User.DisplayName,
                        AvatarUrl = p.User.AvatarUrl ?? string.Empty,
                        Screen = p.Screen
                    })
                    .ToList()
            };
        }
    }
}
