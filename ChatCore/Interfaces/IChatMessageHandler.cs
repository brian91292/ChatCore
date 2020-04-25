using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatMessageHandler
    {
        void OnMessageReceived(IChatMessage messasge);
    }
}
