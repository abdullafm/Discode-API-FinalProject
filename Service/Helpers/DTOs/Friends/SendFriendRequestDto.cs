using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Friends
{
    public class SendFriendRequestDto
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
    }
}
