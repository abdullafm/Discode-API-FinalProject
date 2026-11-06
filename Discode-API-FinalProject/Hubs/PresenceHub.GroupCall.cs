using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.Exceptions;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        public async Task<GroupCallDto> StartGroupCall(int groupId, bool video)
        {
            try
            {
                var change = await _groupCallService.StartAsync(UserId, groupId, video);
                await AnnounceGroupCallAsync(Clients, change);
                if (change.Started)
                {
                    await Clients.Users(change.MemberIds.Where(id => id != UserId)).SendAsync("GroupCallIncoming", change.Call);
                }

                return change.Call;
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task<GroupCallDto> JoinGroupCall(int callId)
        {
            try
            {
                var change = await _groupCallService.JoinAsync(UserId, callId);
                await AnnounceGroupCallAsync(Clients, change);
                return change.Call;
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task RingGroupCallMember(int callId, string userName)
        {
            try
            {
                var ring = await _groupCallService.RingAsync(UserId, callId, userName);
                await Clients.User(ring.UserId).SendAsync("GroupCallIncoming", ring.Call);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task StopRingingGroupCallMember(int callId, string userName)
        {
            try
            {
                var userId = await _groupCallService.StopRingingAsync(UserId, callId, userName);
                await Clients.User(userId).SendAsync("GroupCallRingStopped", new { callId });
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public Task<IEnumerable<string>> GetGroupCallRinging(int callId)
        {
            return _groupCallService.GetRingingAsync(UserId, callId);
        }

        public Task DeclineGroupCall(int callId)
        {
            return _groupCallService.DeclineAsync(UserId, callId);
        }

        public async Task LeaveGroupCall(int callId)
        {
            var change = await _groupCallService.LeaveAsync(UserId, callId);
            if (change is not null)
            {
                await AnnounceGroupCallAsync(Clients, change);
            }
        }

        public async Task SetGroupCallScreen(int callId, bool on)
        {
            var change = await _groupCallService.SetScreenAsync(UserId, callId, on);
            if (change is not null)
            {
                await AnnounceGroupCallAsync(Clients, change);
            }
        }

        public async Task SendGroupCallSignal(int callId, string toUserId, string payload)
        {
            if (string.IsNullOrEmpty(payload) || payload.Length > MaxCallSignalLength || toUserId == UserId)
            {
                return;
            }

            if (await _groupCallService.AreTogetherAsync(UserId, toUserId, callId))
            {
                await Clients.User(toUserId).SendAsync("GroupCallSignal", new { callId, fromUserId = UserId, payload });
            }
        }

        public Task<GroupCallDto?> GetMyGroupCall()
        {
            return _groupCallService.GetMineAsync(UserId);
        }

        public Task<IEnumerable<GroupCallDto>> GetGroupCalls()
        {
            return _groupCallService.GetActiveInMyGroupsAsync(UserId);
        }

        internal static async Task AnnounceGroupCallAsync(IHubClients<IClientProxy> clients, GroupCallChangeDto change)
        {
            foreach (var left in change.Left)
            {
                await AnnounceGroupCallAsync(clients, left);
            }

            var members = clients.Users(change.MemberIds);
            await members.SendAsync("GroupCallUpdated", change.Call);
            if (change.Message is not null)
            {
                var groupId = change.Call.GroupId;
                if (change.Started)
                {
                    await members.SendAsync("GroupMessage", new { groupId, message = change.Message });
                }
                else
                {
                    await members.SendAsync("GroupMessageUpdated", new { groupId, message = change.Message });
                }
            }
        }
    }
}
