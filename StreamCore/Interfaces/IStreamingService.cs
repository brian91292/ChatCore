using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        /// <summary>
        /// The display name of the service
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Callback that occurs when a successful login to the provided streaming service occurs 
        /// </summary>
        event Action<IStreamingService> OnLogin;

        /// <summary>
        /// Callback that occurs when a text message is received
        /// </summary>
        event Action<IStreamingService, IChatMessage> OnTextMessageReceived;

        /// <summary>
        /// Callback that occurs when the user joins a chat channel
        /// </summary>
        event Action<IStreamingService, IChatChannel> OnJoinChannel;

        /// <summary>
        /// Callback that occurs when a chat channel receives updated info
        /// </summary>
        event Action<IStreamingService, IChatChannel> OnRoomStateUpdated;

        /// <summary>
        /// Callback that occurs when the user leaves a chat channel
        /// </summary>
        event Action<IStreamingService, IChatChannel> OnLeaveChannel;

        /// <summary>
        /// Callback that occurs when a users chat is cleared. If null, that means the entire chat was cleared; otherwise the argument is a user id.
        /// </summary>
        event Action<IStreamingService, string> OnChatCleared;

        /// <summary>
        /// Callback that occurs when a specific chat message is cleared. Argument is the message id to be cleared.
        /// </summary>
        event Action<IStreamingService, string> OnMessageCleared;

        /// <summary>
        /// Sends a text message to the specified IChatChannel
        /// </summary>
        /// <param name="message">The text message to be sent</param>
        /// <param name="channel">The chat channel to send the message to</param>
        void SendTextMessage(string message, IChatChannel channel);
    }
}
