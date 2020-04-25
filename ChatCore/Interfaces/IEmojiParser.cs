using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IEmojiParser
    {
        List<IChatEmote> FindEmojis(string str);
    }
}
