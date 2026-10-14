using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.Settings
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string From { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
