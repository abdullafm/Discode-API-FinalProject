using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class FriendRequest : BaseEntity
    {
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
        public AppUser? Sender { get; set; }
        public AppUser? Receiver { get; set; }
    }
}
