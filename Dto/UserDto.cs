using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioBooksToGo.Dto
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string UserRole { get; set; }
        public string ClientId { get; set; }
    }

    public class RefreshDto
    {
        public string UserName { get; set; }
        public string RefreshTokenValue { get; set; }
        public string ClientID { get; set; }
    }
}
