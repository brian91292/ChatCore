using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatUser
    {
        string Id { get; }
        string UserName { get; }
        string DisplayName { get; }
        string Color { get; }
        bool IsBroadcaster { get; }
        bool IsModerator { get; }
        IChatBadge[] Badges { get; }
    }
}
