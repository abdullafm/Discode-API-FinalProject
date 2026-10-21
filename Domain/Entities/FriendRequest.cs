using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class FriendRequest : BaseEntity
    {
        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;
        public string ReceiverId { get; set; } = string.Empty;
        public AppUser Receiver { get; set; } = null!;
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    }
}
