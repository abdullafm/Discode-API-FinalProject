using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
