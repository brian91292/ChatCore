using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.OAuth
{
    public class OAuthCredentials
    {
        public string AccessToken;
        public string RefreshToken;
        public DateTime ExpiresAt;
    }
}
