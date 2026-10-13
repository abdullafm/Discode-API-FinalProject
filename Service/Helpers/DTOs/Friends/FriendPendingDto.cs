namespace Service.Helpers.DTOs.Friends
{
    public class FriendPendingDto
    {
        public IReadOnlyList<FriendRequestDto> Sent { get; set; } = Array.Empty<FriendRequestDto>();
        public IReadOnlyList<FriendRequestDto> Received { get; set; } = Array.Empty<FriendRequestDto>();
    }
}
