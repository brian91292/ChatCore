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
        [ConfigMeta(Comment = "A comma-separated list of Twitch channels to join when the Twitch services are started.")]
        public List<string> Twitch_Channels = new List<string>();
    }
}
