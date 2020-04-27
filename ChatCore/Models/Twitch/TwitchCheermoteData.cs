using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class CheermoteTier
    {
        public string Uri;
        public int MinBits;
        public string Color;
        public bool CanCheer;
    }

    public class TwitchCheermoteData
    {
        public string Prefix;
        public List<CheermoteTier> Tiers = new List<CheermoteTier>();

        public CheermoteTier GetTier(int numBits)
        {
            for (int i = 1; i < Tiers.Count; i++)
            {
                if (numBits < Tiers[i].MinBits)
                    return Tiers[i - 1];
            }
            return Tiers[0];
        }
    }
}
