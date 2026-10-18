using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Live;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class LiveService : ILiveService
    {
        private readonly ILiveStreamRepository _streams;
        private readonly ILiveTrackerRepository _tracker;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<StartLiveDto> _startValidator;
        private readonly IValidator<LiveThumbnailDto> _thumbnailValidator;
        private readonly IValidator<string> _messageValidator;
        private readonly IMapper _mapper;

        public LiveService(ILiveStreamRepository streams,
                           ILiveTrackerRepository tracker,
                           IFriendRequestRepository friendRequests,
                           IAccountRepository accounts,
                           IValidator<StartLiveDto> startValidator,
                           IValidator<LiveThumbnailDto> thumbnailValidator,
                           IValidator<string> messageValidator,
                           IMapper mapper)
        {
            _streams = streams;
            _tracker = tracker;
            _friendRequests = friendRequests;
            _accounts = accounts;
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

            await _tracker.AddViewerAsync(streamId, connectionId);
            var friendIds = await GetFriendIdsAsync(viewerId);

            return new LiveJoinDto
            {
                Stream = await ToDtoAsync(stream, viewerId, friendIds),
                HostConnectionId = hostConnection,
                Viewer = new LiveViewerDto
                {
                    StreamId = streamId,
                    UserName = viewer.UserName ?? string.Empty,
                    AvatarUrl = viewer.AvatarUrl ?? string.Empty
                }
            };
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

        public async Task<LiveMessageDto> CreateMessageAsync(string userId, string connectionId, int streamId, string text)
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
                Text = text!.Trim()
            };
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
