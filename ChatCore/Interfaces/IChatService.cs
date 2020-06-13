using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatService
    {
        /// <summary>
        /// The display name of the service(s)
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Callback that occurs when a successful login to the provided streaming service occurs 
        /// </summary>
        event Action<IChatService> OnLogin;

        /// <summary>
        /// Callback that occurs when a text message is received
        /// </summary>
        event Action<IChatService, IChatMessage> OnTextMessageReceived;

        /// <summary>
        /// Callback that occurs when the user joins a chat channel
        /// </summary>
        event Action<IChatService, IChatChannel> OnJoinChannel;

        /// <summary>
        /// Callback that occurs when a chat channel receives updated info
        /// </summary>
        event Action<IChatService, IChatChannel> OnRoomStateUpdated;

        /// <summary>
        /// Callback that occurs when the user leaves a chat channel
        /// </summary>
        event Action<IChatService, IChatChannel> OnLeaveChannel;

        /// <summary>
        /// Callback that occurs when a users chat is cleared. If null, that means the entire chat was cleared; otherwise the argument is a user id.
        /// </summary>
        event Action<IChatService, string> OnChatCleared;

        /// <summary>
        /// Callback that occurs when a specific chat message is cleared. Argument is the message id to be cleared.
        /// </summary>
        event Action<IChatService, string> OnMessageCleared;

        /// <summary>
        /// Fired once all known resources have been cached for this channel
        /// </summary>
        event Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>> OnChannelResourceDataCached;

        /// <summary>
        /// Sends a text message to the specified IChatChannel
        /// </summary>
        /// <param name="message">The text message to be sent</param>
        /// <param name="channel">The chat channel to send the message to</param>
        void SendTextMessage(string message, IChatChannel channel);
    }
}
