using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Calls;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CallService : ICallService
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static readonly TimeSpan RingLimit = TimeSpan.FromSeconds(60);

        private readonly ICallRepository _calls;
        private readonly IAccountRepository _accounts;
        private readonly IUserBlockRepository _blocks;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IPresenceTrackerRepository _tracker;
        private readonly IDirectMessageService _directMessages;

        public CallService(ICallRepository calls,
                           IAccountRepository accounts,
                           IUserBlockRepository blocks,
                           IFriendRequestRepository friendRequests,
                           IPresenceTrackerRepository tracker,
                           IDirectMessageService directMessages)
        {
            _calls = calls;
            _directMessages = directMessages;
            _accounts = accounts;
            _blocks = blocks;
            _friendRequests = friendRequests;
            _tracker = tracker;
        }

        public async Task<CallPairDto> StartAsync(string callerId, string userName, bool video)
        {
            var callee = await _accounts.GetByUserNameAsync(userName.Trim());
            var caller = await _accounts.GetByIdAsync(callerId);
            if (callee is null || caller is null || callee.Id == callerId)
            {
                throw new NotFoundException("User not found.");
            }

            var calleeName = string.IsNullOrWhiteSpace(callee.DisplayName) ? callee.UserName ?? string.Empty : callee.DisplayName;

            if (await _blocks.IsBlockedEitherWayAsync(callerId, callee.Id))
            {
                throw new BadRequestException("You cannot call this user.");
            }

            await Gate.WaitAsync();
            try
            {
                var ongoing = await _calls.GetOpenBetweenAsync(callerId, callee.Id);
                if (ongoing is not null && ongoing.Active && !IsIn(ongoing, callerId) && IsIn(ongoing, callee.Id))
                {
                    SetIn(ongoing, callerId, true);
                    ongoing.AnsweredAt ??= DateTime.UtcNow;
                    await _calls.UpdateAsync(ongoing);
                    var rejoin = ToPair(ongoing);
                    rejoin.Rejoined = true;
                    return rejoin;
                }
            }
            finally
            {
                Gate.Release();
            }

            if (!callee.IsPublic)
            {
                var friendship = await _friendRequests.GetBetweenAsync(callerId, callee.Id);
                if (friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("This profile is private. You can call them once you are friends.");
                }
            }

            CallEndedDto? leftPrevious = null;
            Call? previous;
            await Gate.WaitAsync();
            try
            {
                await PurgeAsync();
                previous = await FindForAsync(callerId);
                if (await FindForAsync(callee.Id) is not null)
                {
                    throw new BadRequestException(calleeName + " is already in a call.");
                }
            }
            finally
            {
                Gate.Release();
            }

            if (previous is not null)
            {
                leftPrevious = await EndAsync(callerId, previous.Id.ToString(), false);
            }

            var state = new Call
            {
                CallerId = callerId,
                CalleeId = callee.Id,
                Video = video
            };

            await Gate.WaitAsync();
            try
            {
                if (await FindForAsync(callerId) is not null)
                {
                    throw new BadRequestException("You are already in a call.");
                }

                if (await FindForAsync(callee.Id) is not null)
                {
                    throw new BadRequestException(calleeName + " is already in a call.");
                }

                await _calls.AddAsync(state);
                state = (await _calls.GetOpenByIdAsync(state.Id))!;
            }
            finally
            {
                Gate.Release();
            }

            try
            {
                var sent = await _directMessages.StartCallMessageAsync(callerId, callee.Id);
                state.MessageId = sent.MessageForSender.Id;
                await _calls.UpdateAsync(state);
                var pair = ToPair(state);
                pair.Message = sent;
                pair.LeftPrevious = leftPrevious;
                return pair;
            }
            catch
            {
                state.EndedAt = DateTime.UtcNow;
                await _calls.UpdateAsync(state);
                throw;
            }
        }

        public async Task<CallPairDto> AcceptAsync(string userId, string callId)
        {
            await Gate.WaitAsync();
            try
            {
                var state = await FindOpenAsync(callId);
                if (state is null || state.CalleeId != userId || state.Active)
                {
                    throw new NotFoundException("This call is no longer available.");
                }

                state.Active = true;
                state.AnsweredAt = DateTime.UtcNow;
                await _calls.UpdateAsync(state);
                return ToPair(state);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<CallEndedDto?> EndAsync(string userId, string callId, bool timedOut)
        {
            Call state;
            string reason;
            Call? declinedState = null;
            await Gate.WaitAsync();
            try
            {
                var found = await FindOpenAsync(callId);
                if (found is null || (found.CallerId != userId && found.CalleeId != userId))
                {
                    return null;
                }

                state = found;
                if (state.Active && !IsIn(state, userId))
                {
                    return null;
                }

                if (state.Active && IsIn(state, PeerOf(state, userId)))
                {
                    SetIn(state, userId, false);
                    await _calls.UpdateAsync(state);
                    return ToLeft(state, userId);
                }

                if (!state.Active && (state.CalleeId == userId || (state.CallerId == userId && timedOut)))
                {
                    state.Active = true;
                    state.CalleeIn = false;
                    state.CalleeJoinable = true;
                    await _calls.UpdateAsync(state);
                    reason = state.CalleeId == userId ? "declined" : "missed";
                    declinedState = state;
                }
                else
                {
                    state.EndedAt = DateTime.UtcNow;
                    await _calls.UpdateAsync(state);
                    reason = state.Active ? "ended" : "cancelled";
                }
            }
            finally
            {
                Gate.Release();
            }

            if (declinedState is not null)
            {
                var declined = ToLeft(declinedState, declinedState.CalleeId);
                declined.Reason = reason;
                declined.LeftUserCall = ToDto(declinedState, declinedState.CalleeId);
                if (declinedState.MessageId > 0)
                {
                    declined.Message = await _directMessages.FinishCallMessageAsync(declinedState.CallerId, declinedState.CalleeId, declinedState.MessageId, null, true);
                }

                return declined;
            }

            return await FinishAsync(state, reason);
        }

        public async Task<IEnumerable<CallEndedDto>> EndAllForAsync(string userId)
        {
            var results = new List<CallEndedDto>();
            var finishing = new List<Call>();
            await Gate.WaitAsync();
            try
            {
                foreach (var state in await _calls.GetOpenForUserAsync(userId))
                {
                    if (state.Active)
                    {
                        if (!IsIn(state, userId))
                        {
                            continue;
                        }

                        if (IsIn(state, PeerOf(state, userId)))
                        {
                            SetIn(state, userId, false);
                            await _calls.UpdateAsync(state);
                            results.Add(ToLeft(state, userId));
                            continue;
                        }
                    }

                    state.EndedAt = DateTime.UtcNow;
                    await _calls.UpdateAsync(state);
                    finishing.Add(state);
                }
            }
            finally
            {
                Gate.Release();
            }

            foreach (var state in finishing)
            {
                results.Add(await FinishAsync(state, "ended"));
            }

            return results;
        }

        public async Task<IEnumerable<CallEndedDto>> EndIfNotAllowedAsync(string userId, string otherUserId)
        {
            var results = new List<CallEndedDto>();
            var between = new List<Call>();
            await Gate.WaitAsync();
            try
            {
                var open = await _calls.GetOpenBetweenAsync(userId, otherUserId);
                if (open is not null)
                {
                    between.Add(open);
                }
            }
            finally
            {
                Gate.Release();
            }

            if (between.Count == 0)
            {
                return results;
            }

            var allowed = !await _blocks.IsBlockedEitherWayAsync(userId, otherUserId);
            if (allowed)
            {
                var first = await _accounts.GetByIdAsync(userId);
                var second = await _accounts.GetByIdAsync(otherUserId);
                var friendship = await _friendRequests.GetBetweenAsync(userId, otherUserId);
                var friends = friendship is not null && friendship.Status == FriendRequestStatus.Accepted;
                allowed = friends || (first is not null && second is not null && first.IsPublic && second.IsPublic);
            }

            if (allowed)
            {
                return results;
            }

            await Gate.WaitAsync();
            try
            {
                foreach (var state in between)
                {
                    state.EndedAt = DateTime.UtcNow;
                    await _calls.UpdateAsync(state);
                }
            }
            finally
            {
                Gate.Release();
            }

            foreach (var state in between)
            {
                results.Add(await FinishAsync(state, "ended"));
            }

            return results;
        }

        private async Task<CallEndedDto> FinishAsync(Call state, string reason)
        {
            var ended = ToEnded(state, reason);
            if (state.MessageId > 0)
            {
                int? seconds = state.AnsweredAt is null ? null : Math.Max(1, (int)(DateTime.UtcNow - state.AnsweredAt.Value).TotalSeconds);
                ended.Message = await _directMessages.FinishCallMessageAsync(state.CallerId, state.CalleeId, state.MessageId, seconds, state.AnsweredAt is null);
            }

            return ended;
        }

        public async Task<CallDto?> GetForAsync(string userId)
        {
            await Gate.WaitAsync();
            try
            {
                await PurgeAsync();
                var state = await FindForAsync(userId);
                if (state is null)
                {
                    var open = await _calls.GetOpenForUserAsync(userId);
                    state = open.FirstOrDefault(x => x.Active && IsJoinable(x, userId) && IsIn(x, PeerOf(x, userId)));
                }

                return state is null ? null : ToDto(state, userId);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<CallScreenDto?> SetScreenAsync(string userId, string callId, bool on)
        {
            await Gate.WaitAsync();
            try
            {
                var state = await FindOpenAsync(callId);
                if (state is null || !IsIn(state, userId))
                {
                    return null;
                }

                if (state.CallerId == userId)
                {
                    state.CallerScreen = on;
                }
                else
                {
                    state.CalleeScreen = on;
                }

                await _calls.UpdateAsync(state);
                var peerId = PeerOf(state, userId);
                return new CallScreenDto
                {
                    PeerId = peerId,
                    PeerIn = state.Active && IsIn(state, peerId),
                    PeerView = ToDto(state, peerId)
                };
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<int?> GetOngoingMessageIdAsync(string userId, string otherUserId)
        {
            var ongoing = await _calls.GetOpenBetweenAsync(userId, otherUserId);
            return ongoing is not null && ongoing.Active && ongoing.MessageId > 0 && (ongoing.CallerIn || ongoing.CalleeIn)
                ? ongoing.MessageId
                : null;
        }

        public async Task<string?> GetPeerIdAsync(string userId, string callId)
        {
            var state = await FindOpenAsync(callId);
            if (state is null || !state.Active)
            {
                return null;
            }

            if (state.CallerId != userId && state.CalleeId != userId)
            {
                return null;
            }

            var peerId = PeerOf(state, userId);
            return IsIn(state, userId) && IsIn(state, peerId) ? peerId : null;
        }

        private async Task<Call?> FindOpenAsync(string callId)
        {
            return int.TryParse(callId, out var id) ? await _calls.GetOpenByIdAsync(id) : null;
        }

        private async Task PurgeAsync()
        {
            await _calls.EndExpiredRingingAsync(DateTime.UtcNow - RingLimit);
        }

        private async Task<Call?> FindForAsync(string userId)
        {
            var open = await _calls.GetOpenForUserAsync(userId);
            return open.FirstOrDefault(x => IsIn(x, userId));
        }

        private static bool IsIn(Call state, string userId)
        {
            return state.CallerId == userId ? state.CallerIn : state.CalleeId == userId && state.CalleeIn;
        }

        private static void SetIn(Call state, string userId, bool value)
        {
            if (state.CallerId == userId)
            {
                state.CallerIn = value;
                if (!value)
                {
                    state.CallerScreen = false;
                }

                state.CallerJoinable = !value;
            }
            else if (state.CalleeId == userId)
            {
                state.CalleeIn = value;
                if (!value)
                {
                    state.CalleeScreen = false;
                }

                state.CalleeJoinable = !value;
            }
        }

        private static bool IsJoinable(Call state, string userId)
        {
            return state.CallerId == userId ? state.CallerJoinable : state.CalleeId == userId && state.CalleeJoinable;
        }

        private static string PeerOf(Call state, string userId)
        {
            return state.CallerId == userId ? state.CalleeId : state.CallerId;
        }

        private static CallEndedDto ToLeft(Call state, string userId)
        {
            var left = ToEnded(state, "left");
            left.LeftUserId = userId;
            left.LeftUserCall = ToDto(state, userId);
            return left;
        }

        private static CallPartyDto ToParty(AppUser user)
        {
            return new CallPartyDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName,
                AvatarUrl = user.AvatarUrl ?? string.Empty
            };
        }

        private static CallDto ToDto(Call state, string viewerId)
        {
            var isCaller = state.CallerId == viewerId;
            return new CallDto
            {
                CallId = state.Id.ToString(),
                Status = state.Active ? "active" : "ringing",
                Role = isCaller ? "caller" : "callee",
                Video = state.Video,
                PeerPresent = !state.Active || (isCaller ? state.CalleeIn : state.CallerIn),
                Joined = !state.Active || IsIn(state, viewerId),
                PeerScreen = isCaller ? state.CalleeScreen && state.CalleeIn : state.CallerScreen && state.CallerIn,
                Other = ToParty(isCaller ? state.Callee : state.Caller)
            };
        }

        private static CallPairDto ToPair(Call state)
        {
            return new CallPairDto
            {
                CallerId = state.CallerId,
                CalleeId = state.CalleeId,
                ForCaller = ToDto(state, state.CallerId),
                ForCallee = ToDto(state, state.CalleeId)
            };
        }

        private static CallEndedDto ToEnded(Call state, string reason)
        {
            return new CallEndedDto
            {
                CallId = state.Id.ToString(),
                Reason = reason,
                CallerId = state.CallerId,
                CalleeId = state.CalleeId,
                MessageId = state.MessageId
            };
        }
    }
}
