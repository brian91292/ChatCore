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

        [ConfigSection("Mixer")]
        [ConfigMeta(Comment = "The temporary OAuth access token associated with your Mixer account.")]
        public string Mixer_AccessToken = "";
        [ConfigMeta(Comment = "The OAuth refresh token associated with your Mixer account. Generate this in the webapp!")]
        public string Mixer_RefreshToken = "";
        [ConfigMeta(Comment = "The UTC timestamp indicating when the AccessToken will expire.")]
        public DateTime Mixer_ExpiresAt = DateTime.UtcNow;
        [ConfigMeta(Comment = "A comma-separated list of Mixer channels to join when the Mixer services are started.")]
        public List<string> Mixer_Channels = new List<string>();
    }
}
