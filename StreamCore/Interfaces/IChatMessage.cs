using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessage
    {
        string Message { get; }
        IChatUser Sender { get; }
        IChatChannel Channel { get; }
        ReadOnlyDictionary<string, string> Metadata { get; }
    }
}
