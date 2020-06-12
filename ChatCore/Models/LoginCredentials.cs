using ChatCore.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public class LoginCredentials
    {
        [ConfigSection("Twitch")]
        [ConfigMeta(Comment = "The OAuth token associated with your Twitch account. Grab it from https://twitchapps.com/tmi/")]
        public string Twitch_OAuthToken = "";
        public List<string> Twitch_Channels = new List<string>();

        [HTMLIgnore]
        [ConfigSection("Mixer")]
        public string Mixer_AccessToken = "";
        [HTMLIgnore]
        public string Mixer_RefreshToken = "";
        [HTMLIgnore]
        public DateTime Mixer_ExpiresAt = DateTime.UtcNow;
        [HTMLIgnore]
        public List<string> Mixer_Channels = new List<string>();
    }
}
