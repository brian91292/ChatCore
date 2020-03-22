using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        event Action<IChatMessage> OnMessageReceived;
        event Action<IChatChannel> OnJoinRoom;
        event Action<IChatChannel> OnRoomStateUpdated;
        event Action<IChatChannel> OnLeaveRoom;

        void SendTextMessage(string message, string channel = null);
    }
}
