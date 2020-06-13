using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.OAuth
{

    public class OAuthShortcodeRequest
    {
        public string Code { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public int PollFrequencyMs { get; set; } = 5000;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
    }
}
