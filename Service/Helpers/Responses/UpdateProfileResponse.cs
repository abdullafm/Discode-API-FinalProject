using Service.Helpers.DTOs.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.Responses
{
    public class UpdateProfileResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public ProfileDto? Profile { get; set; }
    }
}
