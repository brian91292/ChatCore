using ChatCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ChatCore.Services
{
    public class ChatServiceBase
    {
        protected ConcurrentDictionary<Assembly, Action<IChatService, IChatMessage>> _onTextMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatMessage>>();
        public event Action<IChatService, IChatMessage> OnTextMessageReceived
        {
            add => _onTextMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onTextMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> _onJoinRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        public event Action<IChatService, IChatChannel> OnJoinChannel
        {
            add => _onJoinRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onJoinRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> _onLeaveRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        public event Action<IChatService, IChatChannel> OnLeaveChannel
        {
            add => _onLeaveRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLeaveRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> _onRoomStateUpdatedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        public event Action<IChatService, IChatChannel> OnRoomStateUpdated
        {
            add => _onRoomStateUpdatedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onRoomStateUpdatedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService>> _onLoginCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService>>();
        public event Action<IChatService> OnLogin
        {
            add => _onLoginCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLoginCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, string>> _onChatClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
        public event Action<IChatService, string> OnChatCleared
        {
            add => _onChatClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onChatClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, string>> _onMessageClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
        public event Action<IChatService, string> OnMessageCleared
        {
            add => _onMessageClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onMessageClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>>> _onChannelResourceDataCached = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>>>();
        public event Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>> OnChannelResourceDataCached
        {
            add => _onChannelResourceDataCached.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onChannelResourceDataCached.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
