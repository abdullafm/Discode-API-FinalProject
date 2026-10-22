using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class FriendRequestService : IFriendRequestService
    {
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IUserBlockRepository _blocks;
        private readonly IAccountRepository _accounts;
        private readonly IPresenceService _presence;
        private readonly INotificationService _notifications;
        private readonly IValidator<SendFriendRequestDto> _validator;
        private readonly IMapper _mapper;

        public FriendRequestService(IFriendRequestRepository friendRequests,
                             IUserBlockRepository blocks,
                             IAccountRepository accounts,
                             IPresenceService presence,
                             INotificationService notifications,
                             IValidator<SendFriendRequestDto> validator,
                             IMapper mapper)
        {
            _friendRequests = friendRequests;
            _blocks = blocks;
            _presence = presence;
            _notifications = notifications;
            _accounts = accounts;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<FriendRequestDto> SendAsync(string senderId, SendFriendRequestDto model)
        {
            await _validator.EnsureValidAsync(model);

            AppUser? receiver = null;
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                receiver = await _accounts.GetByIdAsync(model.UserId.Trim());
            }

            if (receiver is null && !string.IsNullOrWhiteSpace(model.UserName))
            {
                receiver = await _accounts.GetByUserNameAsync(model.UserName.Trim());
            }

            if (receiver is null)
            {
                throw new NotFoundException("No user was found with that username.");
            }

            if (receiver.Id == senderId)
            {
                throw new BadRequestException("You cannot add yourself.");
            }

            if (await _blocks.IsBlockedEitherWayAsync(senderId, receiver.Id))
            {
                throw new BadRequestException("You cannot send a friend request to this user.");
            }

            var existing = await _friendRequests.GetBetweenAsync(senderId, receiver.Id);
            if (existing is not null)
            {
                if (existing.Status == FriendRequestStatus.Accepted)
                {
                    throw new BadRequestException("You are already friends.");
                }
                if (existing.SenderId == senderId)
                {
                    throw new BadRequestException("Friend request already sent.");
                }

                throw new BadRequestException("This user already sent you a request.");
            }

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending
            };

            await _friendRequests.AddAsync(request);
            await _notifications.AddAsync(NotificationType.FriendRequest, senderId, receiver.Id);
            return MapFriend(request.Id, receiver);
        }

        public async Task<FriendPendingDto> GetPendingAsync(string userId)
        {
            var sentRows = await _friendRequests.GetSentPendingAsync(userId);
            var receivedRows = await _friendRequests.GetReceivedPendingAsync(userId);

            return new FriendPendingDto
            {
                Sent = sentRows.Select(x => MapFriend(x.Id, x.Receiver)).ToList(),
                Received = receivedRows.Select(x => MapFriend(x.Id, x.Sender)).ToList()
            };
        }

        public async Task<IEnumerable<FriendRequestDto>> GetAllAsync(string userId)
        {
            var rows = await _friendRequests.GetAcceptedAsync(userId);
            var friends = new List<FriendRequestDto>();

            foreach (var item in rows)
            {
                var other = item.SenderId == userId ? item.Receiver : item.Sender;
                var friend = MapFriend(item.Id, other);
                friend.Status = await _presence.ResolveAsync(other);
                friends.Add(friend);
            }

            return friends;
        }

        public async Task<string> CancelAsync(string userId, int requestId)
        {
            var request = await _friendRequests.GetByIdAsync(requestId);
            if (request is null || request.SenderId != userId || request.Status != FriendRequestStatus.Pending)
            {
                throw new NotFoundException("Friend request not found.");
            }

            await _friendRequests.DeleteAsync(request);
            await _notifications.RemoveAsync(NotificationType.FriendRequest, request.SenderId, request.ReceiverId);
            return GetOtherUserId(request, userId);
        }

        public async Task<string> AcceptAsync(string userId, int requestId)
        {
            var request = await _friendRequests.GetByIdAsync(requestId);
            if (request is null || request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            {
                throw new NotFoundException("Friend request not found.");
            }

            request.Status = FriendRequestStatus.Accepted;
            await _friendRequests.UpdateAsync(request);
            await _notifications.RemoveAsync(NotificationType.FriendRequest, request.SenderId, request.ReceiverId);
            await _notifications.AddAsync(NotificationType.FriendAccepted, userId, request.SenderId);
            return GetOtherUserId(request, userId);
        }

        public async Task<string> DeclineAsync(string userId, int requestId)
        {
            var request = await _friendRequests.GetByIdAsync(requestId);
            if (request is null || request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            {
                throw new NotFoundException("Friend request not found.");
            }

            await _friendRequests.DeleteAsync(request);
            await _notifications.RemoveAsync(NotificationType.FriendRequest, request.SenderId, request.ReceiverId);
            return GetOtherUserId(request, userId);
        }

        public async Task<IEnumerable<FriendRequestDto>> GetUserFriendsAsync(string viewerId, string userName)
        {
            var target = await _accounts.GetByUserNameAsync((userName ?? string.Empty).Trim());
            if (target is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (target.Id != viewerId)
            {
                if (await _blocks.IsBlockedEitherWayAsync(viewerId, target.Id))
                {
                    throw new NotFoundException("User not found.");
                }

                var between = await _friendRequests.GetBetweenAsync(viewerId, target.Id);
                var friends = between is not null && between.Status == FriendRequestStatus.Accepted;
                if (!target.IsPublic && !friends)
                {
                    throw new BadRequestException("This profile is private.");
                }
            }

            var rows = await _friendRequests.GetAcceptedAsync(target.Id);
            var list = new List<FriendRequestDto>();
            foreach (var item in rows)
            {
                var other = item.SenderId == target.Id ? item.Receiver : item.Sender;
                if (await _blocks.IsBlockedEitherWayAsync(viewerId, other.Id))
                {
                    continue;
                }

                list.Add(MapFriend(item.Id, other));
            }

            return list;
        }

        public async Task<string> RemoveAsync(string userId, int requestId)
        {
            var request = await _friendRequests.GetByIdAsync(requestId);
            if (request is null ||
                request.Status != FriendRequestStatus.Accepted ||
                (request.SenderId != userId && request.ReceiverId != userId))
            {
                throw new NotFoundException("Friend not found.");
            }

            await _friendRequests.DeleteAsync(request);
            return GetOtherUserId(request, userId);
        }

        private static string GetOtherUserId(FriendRequest request, string userId)
        {
            return request.SenderId == userId ? request.ReceiverId : request.SenderId;
        }

        private FriendRequestDto MapFriend(int id, AppUser user)
        {
            var dto = _mapper.Map<FriendRequestDto>(user);
            dto.Id = id;
            return dto;
        }
    }
}
