using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Accounts
{
    public class UserSearchDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
