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
        public string Twitch_Channels = "";

        internal string[] Twitch_Channels_Array
        {
            get
            {
                var ret = Twitch_Channels.Replace(" ", "").ToLower().TrimEnd(new char[] { ',' }).Split(new char[] { ',' });
                if (ret.Length == 1 && string.IsNullOrEmpty(ret[0]))
                {
                    return new string[0];
                }
                return ret;
            }
            set
            {
                Twitch_Channels = string.Join(",", value).Replace(" ", "").ToLower().TrimEnd(new char[] { ',' });
            }
        }
    }
}
