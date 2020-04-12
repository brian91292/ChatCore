using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    internal class CheermoteTier
    {
        public string Uri;
        public int MinBits;
        public string Color;
        public bool CanCheer;
    }

    internal class TwitchCheermoteData
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
