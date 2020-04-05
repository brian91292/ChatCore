using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IEmojiParser
    {
        List<IChatEmote> FindEmojis(string str);
    }
}
