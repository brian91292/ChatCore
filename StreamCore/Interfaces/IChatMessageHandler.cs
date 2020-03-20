using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessageHandler
    {
        void OnMessageReceived(IChatMessage messasge);
    }
}
