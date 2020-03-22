using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        event Action<IChatMessage> OnMessageReceived;
        event Action<IChatChannel> OnJoinChannel;
        event Action<IChatChannel> OnChannelStateUpdated;
        event Action<IChatChannel> OnLeaveChannel;

        void SendTextMessage(string message, string channel = null);
    }
}
