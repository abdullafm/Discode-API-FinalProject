using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Friends
{
    public class FriendRequestDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
