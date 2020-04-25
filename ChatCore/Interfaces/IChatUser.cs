using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatUser
    {
        string Id { get; }
        string Name { get; }
        string Color { get; }
        bool IsBroadcaster { get; }
        bool IsModerator { get; }
        IChatBadge[] Badges { get; }
    }
}
