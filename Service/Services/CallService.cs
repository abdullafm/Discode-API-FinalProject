using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Calls;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CallService : ICallService
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<string, CallState> Calls = new();
        private static readonly TimeSpan RingLimit = TimeSpan.FromSeconds(60);

        private readonly IAccountRepository _accounts;
        private readonly IUserBlockRepository _blocks;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IPresenceTrackerRepository _tracker;
        private readonly IDirectMessageService _directMessages;

        public CallService(IAccountRepository accounts,
                           IUserBlockRepository blocks,
                           IFriendRequestRepository friendRequests,
                           IPresenceTrackerRepository tracker,
                           IDirectMessageService directMessages)
        {
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

            lock (Gate)
            {
                var ongoing = Calls.Values.FirstOrDefault(x => x.Active &&
                    ((x.CallerId == callerId && x.CalleeId == callee.Id) || (x.CallerId == callee.Id && x.CalleeId == callerId)));
                if (ongoing is not null && !IsIn(ongoing, callerId) && IsIn(ongoing, callee.Id))
                {
                    SetIn(ongoing, callerId, true);
                    ongoing.AnsweredAt ??= DateTime.UtcNow;
                    var rejoin = ToPair(ongoing);
                    rejoin.Rejoined = true;
                    return rejoin;
                }
            }

            if (!callee.IsPublic)
            {
                var friendship = await _friendRequests.GetBetweenAsync(callerId, callee.Id);
                if (friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("This profile is private. You can call them once you are friends.");
                }
            }

            if (!await _tracker.IsOnlineAsync(callee.Id))
            {
                throw new BadRequestException(calleeName + " is not available right now.");
            }

            var state = new CallState
            {
                Id = Guid.NewGuid().ToString("N"),
                CallerId = callerId,
                CalleeId = callee.Id,
                Video = video,
                StartedAt = DateTime.UtcNow,
                Caller = ToParty(caller),
                Callee = ToParty(callee)
            };

            lock (Gate)
            {
                Purge();
                if (FindFor(callerId) is not null)
                {
                    throw new BadRequestException("You are already in a call.");
                }

                if (FindFor(callee.Id) is not null)
                {
                    throw new BadRequestException(calleeName + " is already in a call.");
                }

                Calls[state.Id] = state;
            }

            try
            {
                var sent = await _directMessages.StartCallMessageAsync(callerId, callee.Id);
                state.MessageId = sent.MessageForSender.Id;
                var pair = ToPair(state);
                pair.Message = sent;
                return pair;
            }
            catch
            {
                lock (Gate)
                {
                    Calls.Remove(state.Id);
                }

                throw;
            }
        }

        public CallPairDto Accept(string userId, string callId)
        {
            lock (Gate)
            {
                if (!Calls.TryGetValue(callId, out var state) || state.CalleeId != userId || state.Active)
                {
                    throw new NotFoundException("This call is no longer available.");
                }

                state.Active = true;
                state.AnsweredAt = DateTime.UtcNow;
                return ToPair(state);
            }
        }

        public async Task<CallEndedDto?> EndAsync(string userId, string callId, bool timedOut)
        {
            CallState state;
            string reason;
            CallState? declinedState = null;
            lock (Gate)
            {
                if (!Calls.TryGetValue(callId, out var found) || (found.CallerId != userId && found.CalleeId != userId))
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
                    return ToLeft(state, userId);
                }

                if (!state.Active && (state.CalleeId == userId || (state.CallerId == userId && timedOut)))
                {
                    state.Active = true;
                    state.CalleeIn = false;
                    state.CalleeJoinable = true;
                    reason = state.CalleeId == userId ? "declined" : "missed";
                    declinedState = state;
                }
                else
                {
                    Calls.Remove(callId);
                    reason = state.Active ? "ended" : "cancelled";
                }
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
            var finishing = new List<CallState>();
            lock (Gate)
            {
                foreach (var state in Calls.Values.Where(x => x.CallerId == userId || x.CalleeId == userId).ToList())
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
                            results.Add(ToLeft(state, userId));
                            continue;
                        }
                    }

                    Calls.Remove(state.Id);
                    finishing.Add(state);
                }
            }

            foreach (var state in finishing)
            {
                results.Add(await FinishAsync(state, "ended"));
            }

            return results;
        }

        private async Task<CallEndedDto> FinishAsync(CallState state, string reason)
        {
            var ended = ToEnded(state, reason);
            if (state.MessageId > 0)
            {
                int? seconds = state.AnsweredAt is null ? null : Math.Max(1, (int)(DateTime.UtcNow - state.AnsweredAt.Value).TotalSeconds);
                ended.Message = await _directMessages.FinishCallMessageAsync(state.CallerId, state.CalleeId, state.MessageId, seconds, state.AnsweredAt is null);
            }

            return ended;
        }

        public CallDto? GetFor(string userId)
        {
            lock (Gate)
            {
                Purge();
                var state = FindFor(userId)
                            ?? Calls.Values.FirstOrDefault(x => x.Active && IsJoinable(x, userId) && IsIn(x, PeerOf(x, userId)));
                return state is null ? null : ToDto(state, userId);
            }
        }

        public int? GetOngoingMessageId(string userId, string otherUserId)
        {
            lock (Gate)
            {
                var ongoing = Calls.Values.FirstOrDefault(x => x.Active && x.MessageId > 0 && (x.CallerIn || x.CalleeIn) &&
                    ((x.CallerId == userId && x.CalleeId == otherUserId) || (x.CallerId == otherUserId && x.CalleeId == userId)));
                return ongoing?.MessageId;
            }
        }

        public string? GetPeerId(string userId, string callId)
        {
            lock (Gate)
            {
                if (!Calls.TryGetValue(callId, out var state) || !state.Active)
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
        }

        private static void Purge()
        {
            var expired = Calls.Values
                .Where(x => !x.Active && DateTime.UtcNow - x.StartedAt > RingLimit)
                .Select(x => x.Id)
                .ToList();
            foreach (var id in expired)
            {
                Calls.Remove(id);
            }
        }

        private static CallState? FindFor(string userId)
        {
            return Calls.Values.FirstOrDefault(x => IsIn(x, userId));
        }

        private static bool IsIn(CallState state, string userId)
        {
            return state.CallerId == userId ? state.CallerIn : state.CalleeId == userId && state.CalleeIn;
        }

        private static void SetIn(CallState state, string userId, bool value)
        {
            if (state.CallerId == userId)
            {
                state.CallerIn = value;
            }
            else if (state.CalleeId == userId)
            {
                state.CalleeIn = value;
            }

            SetJoinable(state, userId, !value);
        }

        private static void SetJoinable(CallState state, string userId, bool value)
        {
            if (state.CallerId == userId)
            {
                state.CallerJoinable = value;
            }
            else if (state.CalleeId == userId)
            {
                state.CalleeJoinable = value;
            }
        }

        private static bool IsJoinable(CallState state, string userId)
        {
            return state.CallerId == userId ? state.CallerJoinable : state.CalleeId == userId && state.CalleeJoinable;
        }

        private static string PeerOf(CallState state, string userId)
        {
            return state.CallerId == userId ? state.CalleeId : state.CallerId;
        }

        private static CallEndedDto ToLeft(CallState state, string userId)
        {
            var left = ToEnded(state, "left");
            left.LeftUserId = userId;
            left.LeftUserCall = ToDto(state, userId);
            return left;
        }

        private static CallPartyDto ToParty(Domain.Entities.AppUser user)
        {
            return new CallPartyDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName,
                AvatarUrl = user.AvatarUrl ?? string.Empty
            };
        }

        private static CallDto ToDto(CallState state, string viewerId)
        {
            var isCaller = state.CallerId == viewerId;
            return new CallDto
            {
                CallId = state.Id,
                Status = state.Active ? "active" : "ringing",
                Role = isCaller ? "caller" : "callee",
                Video = state.Video,
                PeerPresent = !state.Active || (isCaller ? state.CalleeIn : state.CallerIn),
                Joined = !state.Active || IsIn(state, viewerId),
                Other = isCaller ? state.Callee : state.Caller
            };
        }

        private static CallPairDto ToPair(CallState state)
        {
            return new CallPairDto
            {
                CallerId = state.CallerId,
                CalleeId = state.CalleeId,
                ForCaller = ToDto(state, state.CallerId),
                ForCallee = ToDto(state, state.CalleeId)
            };
        }

        private static CallEndedDto ToEnded(CallState state, string reason)
        {
            return new CallEndedDto
            {
                CallId = state.Id,
                Reason = reason,
                CallerId = state.CallerId,
                CalleeId = state.CalleeId,
                MessageId = state.MessageId
            };
        }

        private sealed class CallState
        {
            public string Id { get; set; } = string.Empty;
            public string CallerId { get; set; } = string.Empty;
            public string CalleeId { get; set; } = string.Empty;
            public bool Video { get; set; }
            public bool Active { get; set; }
            public bool CallerIn { get; set; } = true;
            public bool CalleeIn { get; set; } = true;
            public bool CallerJoinable { get; set; }
            public bool CalleeJoinable { get; set; }
            public int MessageId { get; set; }
            public DateTime? AnsweredAt { get; set; }
            public DateTime StartedAt { get; set; }
            public CallPartyDto Caller { get; set; } = new();
            public CallPartyDto Callee { get; set; } = new();
        }
    }
}
