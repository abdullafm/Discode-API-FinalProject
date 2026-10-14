using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Friends
{
    public class FriendPendingDto
    {
        public List<FriendRequestDto> Sent { get; set; } = new List<FriendRequestDto>();
        public List<FriendRequestDto> Received { get; set; } = new List<FriendRequestDto>();
    }
}
