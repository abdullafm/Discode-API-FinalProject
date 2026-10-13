using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Friends;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class FriendService : IFriendService
    {
        private readonly IFriendRequestRepository _friendRequests;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<SendFriendRequestDto> _validator;

        public FriendService(
            IFriendRequestRepository friendRequests,
            IAccountRepository accounts,
            IValidator<SendFriendRequestDto> validator)
        {
            _friendRequests = friendRequests;
            _accounts = accounts;
            _validator = validator;
        }

        public async Task<FriendRequestDto> SendAsync(string senderId, SendFriendRequestDto model)
        {
            var validation = await _validator.ValidateAsync(model);
            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var receiver = await FindReceiverAsync(model);
            if (receiver is null)
                throw new NotFoundException("No user was found with that username.");

            if (receiver.Id == senderId)
                throw new BadRequestException("You cannot add yourself.");

            var existing = await _friendRequests.GetBetweenAsync(senderId, receiver.Id);
            if (existing is not null)
            {
                if (existing.Status == FriendRequestStatus.Accepted)
                    throw new BadRequestException("You are already friends.");
                if (existing.SenderId == senderId)
                    throw new BadRequestException("Friend request already sent.");
                throw new BadRequestException("This user already sent you a request.");
            }

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending
            };

            await _friendRequests.AddAsync(request);
            return ToDto(request.Id, receiver);
        }

        public async Task<FriendPendingDto> GetPendingAsync(string userId)
        {
            var sent = await _friendRequests.GetSentPendingAsync(userId);
            var received = await _friendRequests.GetReceivedPendingAsync(userId);

            return new FriendPendingDto
            {
                Sent = sent.Where(x => x.Receiver is not null).Select(x => ToDto(x.Id, x.Receiver!)).ToList(),
                Received = received.Where(x => x.Sender is not null).Select(x => ToDto(x.Id, x.Sender!)).ToList()
            };
        }

        public async Task<IReadOnlyList<FriendRequestDto>> GetAllAsync(string userId)
        {
            var rows = await _friendRequests.GetAcceptedAsync(userId);
            return rows
                .Select(x =>
                {
                    var other = x.SenderId == userId ? x.Receiver : x.Sender;
                    return other is null ? null : ToDto(x.Id, other);
                })
                .Where(x => x is not null)
                .Cast<FriendRequestDto>()
                .ToList();
        }

        public async Task CancelAsync(string userId, int requestId)
        {
            var request = await GetOwnedPendingAsync(requestId, userId, asSender: true);
            await _friendRequests.DeleteAsync(request);
        }

        public async Task AcceptAsync(string userId, int requestId)
        {
            var request = await GetOwnedPendingAsync(requestId, userId, asSender: false);
            request.Status = FriendRequestStatus.Accepted;
            await _friendRequests.UpdateAsync(request);
        }

        public async Task DeclineAsync(string userId, int requestId)
        {
            var request = await GetOwnedPendingAsync(requestId, userId, asSender: false);
            await _friendRequests.DeleteAsync(request);
        }

        private async Task<AppUser?> FindReceiverAsync(SendFriendRequestDto model)
        {
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                var byId = await _accounts.GetByIdAsync(model.UserId.Trim());
                if (byId is not null)
                    return byId;
            }

            if (string.IsNullOrWhiteSpace(model.UserName))
                return null;

            return await _accounts.GetByUserNameAsync(model.UserName.Trim());
        }

        private async Task<FriendRequest> GetOwnedPendingAsync(int requestId, string userId, bool asSender)
        {
            var request = await _friendRequests.GetByIdAsync(requestId);
            var ownerId = asSender ? request?.SenderId : request?.ReceiverId;
            if (request is null || ownerId != userId || request.Status != FriendRequestStatus.Pending)
                throw new NotFoundException("Friend request not found.");

            return request;
        }

        private static FriendRequestDto ToDto(int id, AppUser user)
            => new()
            {
                Id = id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName,
                AvatarUrl = user.AvatarUrl ?? string.Empty
            };
    }
}
