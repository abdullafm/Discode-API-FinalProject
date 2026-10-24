using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Live;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class LiveStreamService : ILiveStreamService
    {
        private readonly ILiveStreamRepository _streams;
        private readonly ILiveTrackerService _tracker;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly IValidator<StartLiveDto> _startValidator;
        private readonly IValidator<LiveThumbnailDto> _thumbnailValidator;
        private readonly IValidator<string> _messageValidator;
        private readonly IMapper _mapper;

        public LiveStreamService(ILiveStreamRepository streams,
                           ILiveTrackerService tracker,
                           IFriendRequestRepository friendRequests,
                           IAccountRepository accounts,
                           INotificationService notifications,
                           IValidator<StartLiveDto> startValidator,
                           IValidator<LiveThumbnailDto> thumbnailValidator,
                           IValidator<string> messageValidator,
                           IMapper mapper)
        {
            _streams = streams;
            _tracker = tracker;
            _friendRequests = friendRequests;
            _accounts = accounts;
            _notifications = notifications;
            _startValidator = startValidator;
            _thumbnailValidator = thumbnailValidator;
            _messageValidator = messageValidator;
            _mapper = mapper;
        }

        public async Task<LiveStreamDto> StartAsync(string userId, string connectionId, StartLiveDto model)
        {
            await _startValidator.EnsureValidAsync(model);

            var host = await _accounts.GetByIdAsync(userId);
            if (host is null)
            {
                throw new NotFoundException("User not found.");
            }

            var stream = new LiveStream
            {
                HostId = userId,
                Title = model.Title.Trim(),
                Category = model.Category
            };

            await _streams.AddAsync(stream);
            await _tracker.OpenAsync(stream.Id, connectionId);

            stream.Host = host;
            return _mapper.Map<LiveStreamDto>(stream, ForViewer(userId));
        }

        public async Task<IEnumerable<int>> StopAsync(string userId)
        {
            var active = (await _streams.GetActiveByHostAsync(userId)).ToList();
            foreach (var stream in active)
            {
                await EndAsync(stream);
            }

            return active.Select(x => x.Id).ToList();
        }

        public async Task SetThumbnailAsync(string userId, LiveThumbnailDto model)
        {
            await _thumbnailValidator.EnsureValidAsync(model);

            var stream = (await _streams.GetActiveByHostAsync(userId)).FirstOrDefault();
            if (stream is null)
            {
                throw new NotFoundException("You are not live.");
            }

            stream.ThumbnailUrl = model.ImageUrl;
            await _streams.UpdateAsync(stream);
        }

        public async Task<IEnumerable<LiveStreamDto>> GetActiveAsync(string viewerId)
        {
            var streams = await _streams.GetActiveVisibleAsync(viewerId);
            var friendIds = await GetFriendIdsAsync(viewerId);

            var result = new List<LiveStreamDto>();
            foreach (var stream in streams)
            {
                if (await _tracker.GetHostConnectionAsync(stream.Id) is null)
                {
                    continue;
                }

                result.Add(await ToDtoAsync(stream, viewerId, friendIds));
            }

            return result;
        }

        public async Task<LiveInviteResultDto> InviteAsync(string userId, int streamId, IEnumerable<string> userNames)
        {
            var stream = await _streams.GetActiveVisibleByIdAsync(userId, streamId);
            if (stream is null || await _tracker.GetHostConnectionAsync(streamId) is null)
            {
                throw new NotFoundException("This stream is not live.");
            }

            var invited = new List<string>();
            var names = new List<string>();
            foreach (var name in userNames.Select(n => (n ?? string.Empty).Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var friend = await _accounts.GetByUserNameAsync(name);
                if (friend is null || friend.Id == userId)
                {
                    continue;
                }

                var friendship = await _friendRequests.GetBetweenAsync(userId, friend.Id);
                if (friendship is null || friendship.Status != FriendRequestStatus.Accepted)
                {
                    continue;
                }

                await _notifications.AddAsync(NotificationType.LiveInvite, userId, friend.Id, snippet: stream.Title, liveStreamId: stream.Id);
                invited.Add(friend.Id);
                names.Add(friend.UserName ?? name);
            }

            if (invited.Count == 0)
            {
                throw new BadRequestException("Pick at least one friend.");
            }

            return new LiveInviteResultDto
            {
                StreamId = stream.Id,
                Title = stream.Title,
                HostName = string.IsNullOrWhiteSpace(stream.Host?.DisplayName) ? stream.Host?.UserName ?? string.Empty : stream.Host.DisplayName,
                ThumbnailUrl = stream.ThumbnailUrl,
                UserNames = names
            };
        }

        public async Task<LiveJoinDto> JoinAsync(string viewerId, string connectionId, int streamId)
        {
            var stream = await _streams.GetActiveVisibleByIdAsync(viewerId, streamId);
            var hostConnection = stream is null ? null : await _tracker.GetHostConnectionAsync(streamId);
            if (stream is null || hostConnection is null)
            {
                throw new NotFoundException("This stream is not live.");
            }

            if (stream.HostId == viewerId)
            {
                throw new BadRequestException("You are the host of this stream.");
            }

            var viewer = await _accounts.GetByIdAsync(viewerId);
            if (viewer is null)
            {
                throw new NotFoundException("User not found.");
            }

            await _tracker.AddViewerAsync(streamId, connectionId, viewerId);
            var friendIds = await GetFriendIdsAsync(viewerId);

            return new LiveJoinDto
            {
                Stream = await ToDtoAsync(stream, viewerId, friendIds),
                HostConnectionId = hostConnection,
                Guest = await GetGuestDtoAsync(streamId),
                Viewer = new LiveViewerDto
                {
                    StreamId = streamId,
                    UserName = viewer.UserName ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(viewer.DisplayName) ? viewer.UserName ?? string.Empty : viewer.DisplayName,
                    AvatarUrl = viewer.AvatarUrl ?? string.Empty
                }
            };
        }

        public async Task<IReadOnlyList<LiveViewerDto>> GetViewersAsync(string connectionId, int streamId)
        {
            if (!await _tracker.IsParticipantAsync(streamId, connectionId))
            {
                throw new NotFoundException("This stream is not live.");
            }

            var viewers = new List<LiveViewerDto>();
            foreach (var id in await _tracker.GetViewerUserIdsAsync(streamId))
            {
                var user = await _accounts.GetByIdAsync(id);
                if (user is null)
                {
                    continue;
                }

                viewers.Add(new LiveViewerDto
                {
                    StreamId = streamId,
                    UserName = user.UserName ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName,
                    AvatarUrl = user.AvatarUrl ?? string.Empty
                });
            }

            return viewers;
        }

        private async Task<LiveGuestDto?> GetGuestDtoAsync(int streamId)
        {
            var connection = await _tracker.GetGuestAsync(streamId);
            return connection is null ? null : await ToGuestDtoAsync(streamId, connection);
        }

        private async Task<LiveGuestDto?> ToGuestDtoAsync(int streamId, string connectionId)
        {
            var userId = await _tracker.GetViewerUserIdAsync(streamId, connectionId);
            var user = userId is null ? null : await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            return new LiveGuestDto
            {
                StreamId = streamId,
                ConnectionId = connectionId,
                UserName = user.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName,
                AvatarUrl = user.AvatarUrl ?? string.Empty
            };
        }

        public async Task<LiveGuestRequestDto> RequestGuestAsync(string connectionId, int streamId)
        {
            var hostConnection = await _tracker.GetHostConnectionAsync(streamId);
            if (hostConnection is null || hostConnection == connectionId || !await _tracker.IsParticipantAsync(streamId, connectionId))
            {
                throw new NotFoundException("This stream is not live.");
            }

            if (await _tracker.GetGuestAsync(streamId) is not null)
            {
                throw new BadRequestException("Someone is already a guest in this stream.");
            }

            var guest = await ToGuestDtoAsync(streamId, connectionId);
            if (guest is null)
            {
                throw new NotFoundException("User not found.");
            }

            return new LiveGuestRequestDto { HostConnectionId = hostConnection, Guest = guest };
        }

        public async Task<LiveGuestDto?> RespondGuestAsync(string hostConnectionId, int streamId, string targetConnectionId, bool accept)
        {
            if (await _tracker.GetHostConnectionAsync(streamId) != hostConnectionId)
            {
                throw new NotFoundException("This stream is not live.");
            }

            if (!accept)
            {
                return null;
            }

            if (!await _tracker.SetGuestAsync(streamId, targetConnectionId))
            {
                throw new BadRequestException("That viewer cannot join as a guest right now.");
            }

            return await ToGuestDtoAsync(streamId, targetConnectionId);
        }

        public async Task<string?> EndGuestAsync(string connectionId, int streamId)
        {
            var guest = await _tracker.GetGuestAsync(streamId);
            var isHost = await _tracker.GetHostConnectionAsync(streamId) == connectionId;
            if (guest is null || (!isHost && guest != connectionId))
            {
                return null;
            }

            return await _tracker.ClearGuestAsync(streamId);
        }

        public async Task<int> LeaveAsync(string connectionId, int streamId)
        {
            return await _tracker.RemoveViewerAsync(streamId, connectionId);
        }

        public async Task<bool> CanSignalAsync(string connectionId, int streamId, string targetConnectionId)
        {
            return await _tracker.IsParticipantAsync(streamId, connectionId) &&
                   await _tracker.IsParticipantAsync(streamId, targetConnectionId);
        }

        public async Task<LiveMessageDto> CreateMessageAsync(string userId, string connectionId, int streamId, string text, string? replyUserName = null, string? replyText = null)
        {
            await _messageValidator.EnsureValidAsync(text ?? string.Empty);

            if (!await _tracker.IsParticipantAsync(streamId, connectionId))
            {
                throw new NotFoundException("This stream is not live.");
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            return new LiveMessageDto
            {
                StreamId = streamId,
                UserName = user.UserName ?? string.Empty,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                Text = text!.Trim(),
                ReplyToUserName = Clip(replyUserName, 60),
                ReplyToText = Clip(replyText, 100)
            };
        }

        private static string Clip(string? value, int length)
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length > length ? text.Substring(0, length) : text;
        }

        public async Task<LiveDisconnectDto> DisconnectAsync(string connectionId)
        {
            var hosted = (await _tracker.CloseHostedAsync(connectionId)).ToList();
            var viewed = await _tracker.RemoveViewerEverywhereAsync(connectionId);

            foreach (var id in hosted)
            {
                var stream = await _streams.GetByIdAsync(id);
                if (stream is not null && stream.EndedAt is null)
                {
                    stream.EndedAt = DateTime.UtcNow;
                    await _streams.UpdateAsync(stream);
                }
            }

            var counts = new Dictionary<int, int>();
            foreach (var id in viewed)
            {
                counts[id] = await _tracker.CountViewersAsync(id);
            }

            return new LiveDisconnectDto
            {
                EndedStreamIds = hosted,
                ViewerCounts = counts
            };
        }

        private async Task EndAsync(LiveStream stream)
        {
            stream.EndedAt = DateTime.UtcNow;
            await _streams.UpdateAsync(stream);
            await _tracker.CloseAsync(stream.Id);
        }

        private async Task<HashSet<string>> GetFriendIdsAsync(string userId)
        {
            var accepted = await _friendRequests.GetAcceptedAsync(userId);
            return accepted.Select(x => x.SenderId == userId ? x.ReceiverId : x.SenderId).ToHashSet();
        }

        private async Task<LiveStreamDto> ToDtoAsync(LiveStream stream, string viewerId, HashSet<string> friendIds)
        {
            var dto = _mapper.Map<LiveStreamDto>(stream, ForViewer(viewerId));
            dto.IsFriend = friendIds.Contains(stream.HostId);
            dto.ViewerCount = await _tracker.CountViewersAsync(stream.Id);
            return dto;
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
