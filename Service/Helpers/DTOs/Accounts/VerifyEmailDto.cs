using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Accounts
{
    public class VerifyEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
