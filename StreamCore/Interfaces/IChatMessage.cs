using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessage
    {
        string Id { get; }
        bool IsSystemMessage { get; }
        bool IsActionMessage { get; }
        string Message { get; }
        IChatUser Sender { get; }
        IChatChannel Channel { get; }
        IChatEmote[] Emotes { get; }
        ReadOnlyDictionary<string, string> Metadata { get; }
    }
}
