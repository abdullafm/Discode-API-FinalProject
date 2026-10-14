using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Accounts
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
