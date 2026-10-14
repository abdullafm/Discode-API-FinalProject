using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Accounts
{
    public class ChangeUsernameDto
    {
        public string UserName { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
